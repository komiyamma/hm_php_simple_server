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
        static Process proc;
        string workingDirectory;
        int hostPort;
        string hostName;

        Task<string> task;
        CancellationTokenSource cts;

        // PHPデーモンのスタート
        public string Launch(string phpExePath, string hostName, int hostPort)
        {
            try
            {
                this.hostName = hostName;
                this.hostPort = hostPort;

                currMacroFilePath = (String)Hm.Macro.Var["currentmacrofilename"];
                Destroy();

                SetWorkingDirectory();

                proc = new Process();
                ProcessStartInfo psi = proc.StartInfo;
                psi.FileName = phpExePath;
                psi.Arguments = $" -S {this.hostName}:{this.hostPort} -t \"{this.workingDirectory}\" ";

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                proc.OutputDataReceived += Proc_OutputDataReceived;


                proc.Start();

                cts = new CancellationTokenSource();
                CancellationToken ct = cts.Token;
                task = Task.Run(() => {
                    return TickMethodAsync(ct);
                }, ct);

                string filepath = Hm.Edit.FilePath;
                prevFileFullPath = filepath;
                if (!String.IsNullOrEmpty(filepath))
                {
                    return Path.GetFileName(filepath);
                }
            }
            catch (Exception e)
            {
                Hm.OutputPane.Output(e.ToString() + "\r\n");
            }

            return "";
        }

        string prevFileFullPath = null;
        string currMacroFilePath = "";

        // ファイル名が変化したことを検知したら、自分自身を実行する。
        private async Task<string> TickMethodAsync(CancellationToken ct)
        {
            for (int i = 0; i < 5; i++) {
                ct.ThrowIfCancellationRequested();
            }

            while (true)
            {
                await DelayMethod(ct);

                string currFileFullPath = Hm.Edit.FilePath;
                // ファイル名が変化したら、改めて自分自身のマクロを実行する。
                if (prevFileFullPath != currFileFullPath)
                {
                    prevFileFullPath = currFileFullPath;

                    // 同期マクロ実行中ではない
                    if (!Hm.Macro.IsExecuting && !String.IsNullOrEmpty(currFileFullPath))
                    {
                        Hm.OutputPane.Output(currMacroFilePath + "\r\n");
                        // 自分自身を実行
                        Hm.Macro.Exec.File(currMacroFilePath);

                    }
                }

            }
        }

        private static async Task<CancellationToken> DelayMethod(CancellationToken ct)
        {
            await Task.Delay(100);
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

        private void SetWorkingDirectory()
        {
            if (String.IsNullOrWhiteSpace(workingDirectory))
            {
                workingDirectory = Hm.Edit.FilePath ?? "";
            }
            string currFilePath = Hm.Edit.FilePath;
            if (!String.IsNullOrWhiteSpace(currFilePath))
            {
                if (File.Exists(currFilePath))
                {
                    workingDirectory = Path.GetDirectoryName(currFilePath);
                }
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
                if (cts != null)
                {
                    cts.Cancel();
                }
                if (proc != null)
                {
                    proc.Kill();
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
