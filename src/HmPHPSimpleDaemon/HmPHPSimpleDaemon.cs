/*
 * Copyright (c) 2023 Akitsugu Komiyama
 * under the MIT License
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using HmNetCOM;

namespace HmPHPSimpleDaemon
{
    [Guid("B4BE82DB-9F1F-423C-BAA3-39AAC146AB44")]
    public class HmPHPSimpleDaemon
    {
        static Process phpProcess;
        string phpServerDocumentFolder;
        string phpHostName;
        int phpHostPort;

        Task<string> task;
        CancellationTokenSource cts;

        System.IO.FileSystemWatcher watcher;
        int targetBrowserPane = 2;

        // PHPデーモンのスタート
        public string Launch(string phpExePath, string hostName, int hostPort, string parentDocumentRoot)
        {
            try
            {
                this.targetBrowserPane = (int)(dynamic)Hm.Macro.Var["#TARGET_BROWSER_PANE"];
                this.phpHostName = hostName;
                this.phpHostPort = hostPort;
                this.isMustReflesh = false;

                currMacroFilePath = (String)Hm.Macro.Var["currentmacrofilename"];
                Destroy();

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
            string filepath = Hm.Edit.FilePath;
            if (!String.IsNullOrEmpty(filepath))
            {
                var directory = Path.GetDirectoryName(filepath);
                var filename = Path.GetFileName(filepath);
                watcher = new System.IO.FileSystemWatcher(directory, filename);

                //監視するフィールドの設定
                watcher.NotifyFilter = (NotifyFilters.LastWrite);

                //サブディレクトリは監視しない
                watcher.IncludeSubdirectories = false;

                //監視を開始する
                watcher.EnableRaisingEvents = true;
                watcher.Changed += new System.IO.FileSystemEventHandler(watcher_Changed);
            }
        }

        private void DestroyFileWatcher()
        {
            if (watcher != null)
            {
                watcher.Dispose();
            }
        }

        bool isMustReflesh = false;

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (!Hm.Macro.IsExecuting)
            {
                isMustReflesh = true;
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
            phpProcess = new Process();
            ProcessStartInfo psi = phpProcess.StartInfo;
            psi.FileName = phpExePath;
            psi.Arguments = $" -S {this.phpHostName}:{this.phpHostPort} -t \"{this.phpServerDocumentFolder}\" ";

            psi.UseShellExecute = false;
            psi.CreateNoWindow = true;
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            phpProcess.OutputDataReceived += Proc_OutputDataReceived;

            phpProcess.Start();
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

        // ファイル名が変化したことを検知したら、HmPHPSimpleDaemon.mac(自分の呼び出し元)を改めて実行する。
        // これによりマクロにより、このクラスのインスタンスがクリアされるとともに、新たなファイル名、新たなポート番号を使って、PHPサーバーが再起動される。
        private async Task<string> TickMethodAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    await DelayMethod(ct);
                }

                string currFileFullPath = Hm.Edit.FilePath;

                if (String.IsNullOrEmpty(currFileFullPath))
                {

                    string command = $"setbrowserpaneurl \"about:blank\", {targetBrowserPane};";

                    // リフレッシュする
                    Hm.Macro.Exec.Eval(command);
                    Destroy();
                }

                // ファイル名が変化したら、改めて自分自身のマクロを実行する。
                if (prevFileFullPath != currFileFullPath)
                {
                    prevFileFullPath = currFileFullPath;

                    // 同期マクロ実行中ではない
                    if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                    {

                        // 自分自身を実行
                        Hm.Macro.Exec.File(currMacroFilePath);
                    }
                }

                if (isMustReflesh)
                {
                    isMustReflesh = false;

                    // 同期マクロ実行中ではない
                    if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                    {

                        // リフレッシュする
                        Hm.Macro.Exec.Eval($"refreshbrowserpane {targetBrowserPane};");
                    }
                }
            }
        }

        private static async Task<CancellationToken> DelayMethod(CancellationToken ct)
        {
            await Task.Delay(150);
            if (ct.IsCancellationRequested)
            {
                // Clean up here, then...
                ct.ThrowIfCancellationRequested();
            }

            return ct;
        }

        private void Proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                Hm.OutputPane.Output(e.Data + "\r\n");
            }
        }

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

                return 1;
            }
            catch (Exception)
            {

            }


            return 0;
        }
    }
}
