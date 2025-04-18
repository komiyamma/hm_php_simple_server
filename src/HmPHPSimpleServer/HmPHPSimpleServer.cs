/*
 * Copyright (c) 2023 Akitsugu Komiyama
 * under the MIT License
 */

using HmNetCOM;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;


namespace HmPHPSimpleServer
{
    [Guid("557DD52E-8900-4DD5-9203-8F30F7329C8D")]
    public class HmPHPSimpleServer
    {
        static Process phpProcess;
        string phpServerDocumentFolder;
        string phpHostName;
        int phpHostPort;

        Task<bool> task;
        CancellationTokenSource cts;

        System.IO.FileSystemWatcher watcher;
        int targetBrowserPane = 2;
        int notifyFileWatcher = 1;
        string notifyFolderChangeFilter = "";

        // PHPデーモンのスタート
        public string Launch(string phpExePath, string hostName, int hostPort, string parentDocumentRoot)
        {
            try
            {
                Destroy();

                // なにやら有効な上書きのドキュメントルートが指定されている
                if (!String.IsNullOrEmpty(parentDocumentRoot))
                {
                    // だがそのようなディレクトリは存在しない
                    if (!Directory.Exists(parentDocumentRoot))
                    {
                        Hm.OutputPane.Output($"「{parentDocumentRoot}」というディレクトリは存在しません。");
                        return "";
                    }
                }

                this.targetBrowserPane = (int)(dynamic)Hm.Macro.Var["#TARGET_BROWSER_PANE"];
                this.notifyFolderChangeFilter = (string)Hm.Macro.Var["$NOTIFY_FOLDER_CHANGE_FILTER"];
                this.notifyFileWatcher = (int)(dynamic)Hm.Macro.Var["#NOTIFY_FILE_WATCHER"];
                this.phpHostName = hostName;
                this.phpHostPort = hostPort;
                this.isMustReflesh = false;


                currMacroFilePath = (String)Hm.Macro.Var["currentmacrofilename"];

                SetPHPServerDocumentRoot(parentDocumentRoot);

                CreatePHPServerProcess(phpExePath);

                CreateFileWatcher();

                CreateTaskMonitoringFilePath();

                return GetHmEditFileName();
            }
            catch (Exception e)
            {
                Hm.OutputPane.Output(e.ToString() + "\r\n");
            }

            return "";
        }

        private void CreateFileWatcher()
        {
            if (notifyFileWatcher == 0)
            {
                return;
            }

            string filepath = Hm.Edit.FilePath;
            if (String.IsNullOrEmpty(filepath))
            {
                return;
            }

            if (!String.IsNullOrEmpty(notifyFolderChangeFilter))
            {
                watcher = new System.IO.FileSystemWatcher(phpServerDocumentFolder);

                //サブディレクトリは監視しない
                watcher.IncludeSubdirectories = true;

                //監視するフィールドの設定
                watcher.NotifyFilter = (NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName);

                watcher.Changed += watcher_Changed_WithFilter;
                watcher.Deleted += watcher_Changed_WithFilter;
                watcher.Created += watcher_Changed_WithFilter;
                watcher.Renamed += watcher_Changed_WithFilter;
            }
            else
            {
                var directory = Path.GetDirectoryName(filepath);
                var filename = Path.GetFileName(filepath);

                watcher = new System.IO.FileSystemWatcher(directory, filename);

                //監視するフィールドの設定
                watcher.NotifyFilter = (NotifyFilters.LastWrite | NotifyFilters.Size);

                watcher.Changed += watcher_Changed;
            }


            //監視を開始する
            watcher.EnableRaisingEvents = true;
        }

        private void DestroyFileWatcher()
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
        }

        bool isMustReflesh = false;

        private bool IsUnderHiddenDirectory(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    var fileInfo = new FileInfo(path);
                    if ((fileInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden ) {
                        return true;
                    }
                }

                if (Directory.Exists(path))
                {
                    var dirInfo = new DirectoryInfo(path);
                    if ((dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden) {
                        return true;
                    }
                }

                var parent = new DirectoryInfo(Path.GetDirectoryName(path));
                while (parent != null)
                {
                    if (parent.FullName == watcher.Path)
                    {
                        break;
                    }
                    if ((parent.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                    {
                        return true;
                    }
                    parent = parent.Parent;
                }
            }
            catch (Exception)
            {
            }

            return false;
        }

        private void watcher_Changed_WithFilter(object sender, FileSystemEventArgs e)
        {
            try
            {
                if (e.FullPath.Contains("\\.git\\") || e.FullPath.EndsWith("\\.git"))
                {
                    return;
                }

                if (IsUnderHiddenDirectory(e.FullPath))
                {
                    return;
                }

                Regex regex = new Regex(notifyFolderChangeFilter, RegexOptions.IgnoreCase);
                if (regex.IsMatch(e.FullPath))
                {
                    isMustReflesh = true;
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            // このフラグをtrueにするかどうかで調べてるんだkら、trueなら調べる必要性自体がない
            if (isMustReflesh)
            {
                return;
            }
            try
            {
                if (e.FullPath.Contains("\\.git\\") || e.FullPath.EndsWith("\\.git"))
                {
                    return;
                }

                if (IsUnderHiddenDirectory(e.FullPath))
                {
                    return;
                }

                isMustReflesh = true;
            }
            catch (Exception ex)
            {
            }
        }

        // 秀丸で編集中ファイル名をモニターするためのタスク生成
        private void CreateTaskMonitoringFilePath()
        {
            cts = new CancellationTokenSource();
            CancellationToken ct = cts.Token;
            task = Task.Run(() =>
            {
                return TickMethodAsync(ct);
            }, ct);
        }

        // 編集中のファイルのパス文字列の「ファイル名と拡張子」を返す。
        private string GetHmEditFileName()
        {
            string filepath = Hm.Edit.FilePath;
            prevFileFullPath = filepath;
            if (!String.IsNullOrEmpty(filepath))
            {
                var dir = Path.GetDirectoryName(filepath);
                if (dir == phpServerDocumentFolder)
                {
                    return Path.GetFileName(filepath);
                }
                else
                {
                    var uriCurrentEditFilePath = new Uri(filepath).ToString();
                    var uriPhpDocumentRoot = new Uri(phpServerDocumentFolder).ToString();
                    string relative = uriCurrentEditFilePath.Replace(uriPhpDocumentRoot, "");
                    // 置き換えが発生しなかったということは、親子関係のパスになっていない。
                    if (relative == uriCurrentEditFilePath)
                    {
                        string message = $"指定されたサーバーのドキュメントルート「{uriPhpDocumentRoot}」からでは、「{uriCurrentEditFilePath}」は閲覧出来ない可能性が高いです。";
                        Hm.OutputPane.Output(message + "\r\n");
                    }
                    // relativeパスの先頭の/は消す
                    if (relative.Length > 0 && relative[0] == '/')
                    {
                        relative = relative.Substring(1);
                    }
                    return relative.ToString();
                }
            }
            else
            {
                return "";
            }
        }

        // PHPプロセス生成
        private void CreatePHPServerProcess(string phpExePath)
        {
            try
            {
                phpProcess = new Process();
                ProcessStartInfo psi = phpProcess.StartInfo;
                psi.FileName = phpExePath;
                psi.Arguments = $" -S {this.phpHostName}:{this.phpHostPort} -t \"{this.phpServerDocumentFolder}\" ";

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                // phpProcess.OutputDataReceived += Proc_OutputDataReceived;

                phpProcess.Start();
            }
            catch (Exception ex)
            {
                Hm.OutputPane.Output("\"" + phpExePath + "\"" + ":\r\n" + ex.ToString() + "\r\n");
            }
        }

        // PHPが起動する際のドキュメントルート
        private void SetPHPServerDocumentRoot(string parentDocumentRoot)
        {
            if (String.IsNullOrEmpty(parentDocumentRoot))
            {
                string currFilePath = Hm.Edit.FilePath;

                if (String.IsNullOrWhiteSpace(currFilePath))
                {
                    return;
                }

                if (File.Exists(currFilePath))
                {
                    phpServerDocumentFolder = Path.GetDirectoryName(currFilePath);
                }
            }
            else
            {
                if (Directory.Exists(parentDocumentRoot))
                {
                    phpServerDocumentFolder = parentDocumentRoot;
                }
            }
        }

        string prevFileFullPath = null;
        string currMacroFilePath = "";

        // ファイル名が変化したことを検知したら、HmPHPSimpleServer.mac(自分の呼び出し元)を改めて実行する。
        // これによりマクロにより、このクラスのインスタンスがクリアされるとともに、新たなファイル名、新たなポート番号を使って、PHPサーバーが再起動される。
        private async Task<bool> TickMethodAsync(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        if (ct.IsCancellationRequested)
                        {
                            return true;
                        }
                        await DelayMethod(ct);
                    }

                    string currFileFullPath = Hm.Edit.FilePath;

                    if (String.IsNullOrEmpty(currFileFullPath))
                    {

                        string command = $"setbrowserpaneurl \"about:blank\", {targetBrowserPane};";

                        isMustReflesh = false;
                        // 空白のを出して終わる
                        Hm.Macro.Exec.Eval(command);
                        Destroy();

                        return true;
                    }

                    // ファイル名が変化したら、改めて自分自身のマクロを実行する。
                    if (prevFileFullPath != currFileFullPath)
                    {
                        prevFileFullPath = currFileFullPath;

                        // 同期マクロ実行中ではない
                        if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                        {
                            isMustReflesh = false;

                            // 自分自身を実行
                            Hm.Macro.Exec.File(currMacroFilePath);

                            return true;
                        }
                    }

                    if (isMustReflesh)
                    {
                        // 同期マクロ実行中ではない
                        if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                        {
                            // Hm.OutputPane.Output("refreshbrowserpane");

                            // リフレッシュする
                            var ret = Hm.Macro.Exec.Eval($"refreshbrowserpane {targetBrowserPane}; endmacro \"complete\"; ");
                            // if (ret.Message == "complete")
                            {
                                isMustReflesh = false;
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
            }
            return true;
        }

        private static async Task<CancellationToken> DelayMethod(CancellationToken ct)
        {
            int currentWindowBackGround = Hm.Edit.InputStates & 0x00000800;
            // 自分のウィンドウはタブモードでかつ裏に隠れているか？
            if (currentWindowBackGround > 0)
            {
                await Task.Delay(700, ct);
            }
            else
            {
                await Task.Delay(150, ct);
            }

            if (ct.IsCancellationRequested)
            {
                // Clean up here, then...
                ct.ThrowIfCancellationRequested();
            }

            return ct;
        }

        /*
        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Hm.OutputPane.Output(e.Data + "\r\n");
            }
        }
        */

        public void OnReleaseObject(int reason = 0)
        {
            Destroy();
        }

        private long Destroy()
        {
            try
            {
                DestroyFileWatcher();
            }
            catch (Exception)
            {

            }
            try
            {
                if (cts != null)
                {
                    cts.Cancel();
                }
            }
            catch (Exception)
            {

            }
            try
            {
                if (phpProcess != null)
                {
                    phpProcess.Kill();
                }
            }
            catch (Exception)
            {

            }
            try
            {
                if (task != null)
                {
                    task.Dispose();
                }
            }
            catch (Exception)
            {

            }


            return 0;
        }
    }
}
