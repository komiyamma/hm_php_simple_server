/*
 * Copyright (c) 2023 Akitsugu Komiyama
 * under the MIT License
 */

using HmNetCOM;
using Markdig;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;


namespace HmMarkdownSimpleServer;

[Guid("613BF59D-753E-4EEF-BFDE-BC621FDFDA60")]
public class HmMarkdownSimpleServer
{
    Task<string> task;
    CancellationTokenSource cts;

    System.IO.FileSystemWatcher watcher;
    int targetBrowserPane = 2;

    Dictionary<string, TemporaryFile> dic = new Dictionary<string, TemporaryFile>();

    string strPathCSSWin = "";

    int darkmode = 0;

    // Markdon処理のスタート
    public string Launch(string strPathCSS = "")
    {
        try
        {
            Destroy();

            currMacroFilePath = (String)Hm.Macro.Var["currentmacrofilename"];
            darkmode = (int)(dynamic)Hm.Macro.Var["darkmode"];

            string tempFileFullPath = GetTemporaryFileName();
            prevFileFullPath = Hm.Edit.FilePath ?? "";
            CreateFileWatcher();

            CreateTaskMonitoringFilePath();

            isMustReflesh = true;

            return tempFileFullPath;
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
            watcher.NotifyFilter = (NotifyFilters.LastWrite | NotifyFilters.Size);

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
            // Hm.OutputPane.Output("watcher_Changed");
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

    // マークダウンファイル直接だと表示しにくいのでテンポラリファイルをtemp内に作成する。
    private String GetTemporaryFileName()
    {
        try
        {
            TemporaryFile temp;
            string filepath = Hm.Edit.FilePath;
            if (String.IsNullOrEmpty(filepath))
            {
                return "";
            }

            if (dic.ContainsKey(filepath))
            {
                temp = dic[filepath];
            }
            else
            {
                temp = new TemporaryFile();
                dic[filepath] = temp;
            }

            return temp.FullName;
        }
        catch (Exception)
        {
        }
        return "";
    }


    string prevFileFullPath = null;
    string currMacroFilePath = "";

    // ファイル名が変化したことを検知したら、HmMarkdownSimpleServer.mac(自分の呼び出し元)を改めて実行する。
    // これによりマクロにより、このクラスのインスタンスがクリアされるとともに、新たなファイル名を使って、Markdownを新たなファイルに紐づけで表示されることになる。
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
                    isMustReflesh = false;

                    // 自分自身を実行
                     Hm.Macro.Exec.File(currMacroFilePath);
                }
            }

            if (isMustReflesh)
            {

                // Hm.OutputPane.Output("isMustReflesh");
                // 同期マクロ実行中ではない
                if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                {
                    try
                    {
                        string markdowntext = File.ReadAllText(currFileFullPath);

                        var pipeLine = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

                        string html = Markdig.Markdown.ToHtml(markdowntext, pipeLine);
                        string tempFileFullPath = GetTemporaryFileName();
                        string baseDirWin = Path.GetDirectoryName(currFileFullPath);
                        string baseHref = new Uri(baseDirWin).AbsoluteUri;
                        string cssHref = "";
                        if (String.IsNullOrEmpty(strPathCSSWin))
                        {
                            string thisDllDirWin = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            if (darkmode > 0)
                            {
                                strPathCSSWin = thisDllDirWin + "\\HmMarkdownSimpleServerDark.css";
                            }
                            else
                            {
                                strPathCSSWin = thisDllDirWin + "\\HmMarkdownSimpleServerLight.css";
                            }
                            // Hm.OutputPane.Output(strPathCSSWin + "\r\n");

                        }
                        cssHref = new Uri(strPathCSSWin).AbsoluteUri;
                        // Hm.OutputPane.Output(cssHref + "\r\n");
                        // <br>を２つ入れてるのは末尾スクロールが微妙に届かないのを防ぐため。
                        html = "<html><head><link rel='stylesheet' href='"+ cssHref + "'><base href=\"" + baseHref + "/\"></head><body class='markdown-body'>" + html + "<br></body></html>";
                        File.WriteAllText(tempFileFullPath, html);
                    }
                    catch (Exception) { }

                    isMustReflesh = false;

                    // Hm.OutputPane.Output("refreshbrowserpane");

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
            foreach (var item in dic)
            {
                item.Value.Dispose();
            }
            dic.Clear();

            return 1;
        }
        catch (Exception)
        {

        }


        return 0;
    }
}
