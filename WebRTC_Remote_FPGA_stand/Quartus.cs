using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WebRTC_Remote_FPGA_stand
{
    public class Quartus : IDisposable
    {
        private const string environment_variable = "QUARTUS_ROOTDIR";
        private string path_to_quartus { get; }

        private static Process cmd;

        private static Quartus _instance;

        private Quartus()
        {
            cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;


            cmd.Start();
            cmd.StandardInput.WriteLine("SET " + environment_variable);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();

            cmd.StandardOutput.ReadLine();
            cmd.StandardOutput.ReadLine();
            cmd.StandardOutput.ReadLine();
            cmd.StandardOutput.ReadLine();
            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());

            string path = cmd.StandardOutput.ReadLine();
            path_to_quartus = path.Substring(path.IndexOf('=') + 1) + "\\bin64\\";

        }

        static public Quartus GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Quartus();
            }
            return _instance;
        }



        public string RunQuartusCommand(string command)
        {
            // Restart StandardInput
            cmd.Start();

            cmd.StandardInput.WriteLine(path_to_quartus + command);
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();
            return cmd.StandardOutput.ReadToEnd();
        }

        public Task<string> RunQuartusCommandAsync(string command)
        {
            return Task.Run(() => RunQuartusCommand(command));
        }

        public void Dispose() {
            // it means that cmd proccess was created
            if (_instance != null)
            {
                // Kill created process.
                cmd.Kill();
                // Free associated process.
                cmd.Close();
                // Set _instance null, to say GetInstance that to call lazy initialization
                _instance = null;
            }
        }

        public void Close() {
            Dispose();
        }
    }
}