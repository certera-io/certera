using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Certera.Web.Services
{
    public class ProcessRunner
    {
        private const int PROCESS_WAIT_MS = 60000;

        public (int ExitCode, string Output) Run(string executablePath, string arguments, IDictionary<string, string> environmentVariables = default)
        {
            var output = new StringBuilder();
            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            foreach (var item in environmentVariables ?? Enumerable.Empty<KeyValuePair<string, string>>())
            {
                process.StartInfo.Environment.Add(item.Key.Trim(), item.Value.Trim());
            }

            using (process)
            {
                process.Start();
                process.OutputDataReceived += (sender, outputLine) => { if (outputLine.Data != null) output.AppendLine(outputLine.Data); };
                process.ErrorDataReceived += (sender, errorLine) => { if (errorLine.Data != null) output.AppendLine(errorLine.Data); };
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();

                var exited = process.WaitForExit(PROCESS_WAIT_MS);
                if (!exited)
                {
                    process.Kill(true);
                }
                return (ExitCode: process.ExitCode, Output: output.ToString());
            }
            
        }
    }
}


