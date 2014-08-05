using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace TftpUnitTest
{
    public enum WindowsTftpClientRequest
    {
        GET,
        PUT
    }

    public static class WindowsTftpClient
    {
        public static string WindowsTftpClientPath { get { return @"C:\WINDOWS\SYSTEM32\TFTP.EXE"; } }

        public static int Run(
            string server,
            WindowsTftpClientRequest request,
            string source,
            string destination,
            out string stderrLines,
            out string stdoutLines,
            CancellationToken cancellationToken)
        {
            string args = string.Format("-i {0} {1} {2} {3}", server, request.ToString(), source, destination);
            return RunCmd(WindowsTftpClientPath, args, out stderrLines, out stdoutLines, cancellationToken);
        }

        /// <summary>
        /// Runs a command line in a new process and writes the standard out and standard error streams to a file.
        /// Returns the exit code of the command.
        /// </summary>
        private static int RunCmd(string name, string arguments, out string stderrLines, out string stdoutLines, CancellationToken cancellationToken)
        {
            var procStart = new ProcessStartInfo
            {
                FileName = name,
                Arguments = arguments,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            int exitCode;
            using (var proc = new Process())
            {
                proc.StartInfo = procStart;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();

                cancellationToken.Register(() =>
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception ex)
                    {
                        // This could happen in small time window where Process already terminated or was disposed
                        Console.WriteLine(string.Format("Unable to kill process '{0} {1}' due to {2}", name, arguments, ex);
                    }
                });

                proc.WaitForExit();
                cancellationToken.ThrowIfCancellationRequested();
                stdoutLines = proc.StandardOutput.ReadToEnd();
                stderrLines = proc.StandardError.ReadToEnd();
                exitCode = proc.ExitCode;
            }
            return exitCode;
        }
    }
}
