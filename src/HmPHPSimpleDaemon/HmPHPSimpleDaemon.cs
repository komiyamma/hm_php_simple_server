using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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


        public string Launch(string phpExePath, string hostName, int hostPort)
        {
            try
            {
                this.hostName = hostName;
                this.hostPort = hostPort;
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
                string filepath = Hm.Edit.FilePath;
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
