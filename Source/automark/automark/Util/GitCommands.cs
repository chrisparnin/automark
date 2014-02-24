using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace automark.Util
{
    public class GitCommands
    {
        public static void Clone(string url, string dest)
        {
            RunProcess(string.Format(" clone {0} {1}", url, dest));
        }

        private static string RunProcess(string command)
        {
            // Start the child process.
            Process p = new Process();
            // Redirect the output stream of the child process.
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            //p.StartInfo.RedirectStandardError = true;
            p.StartInfo.FileName = Config.GitExectuable;
            p.StartInfo.Arguments = command;
            p.Start();
            // Do not wait for the child process to exit before
            // reading to the end of its redirected stream.
            // p.WaitForExit();
            // Read the output stream first and then wait.
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            //string error = p.StandardError.ReadToEnd();
            //p.WaitForExit();
            return output;
        }

        public static string ListSha(string path)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" log --name-only", path.Replace("\\", "/"), path.Replace("\\", "/")));
            return output;
        }

        public static string ListShaWithFiles(string path)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" log --name-status", path.Replace("\\", "/"), path.Replace("\\", "/")));
            return output;
        }

        public static string ShowDiffRange(string path, string shaBefore, string shaAfter)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" diff {2} {3} --ignore-all-space", path.Replace("\\", "/"), path.Replace("\\", "/"), shaBefore, shaAfter));
            return output;
        }

        public static string ShowSha(string path, string sha)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" show {2} --ignore-all-space", path.Replace("\\", "/"), path.Replace("\\", "/"), sha));
            return output;
        }

        public static string ShowFileAfterCommit(string path, string sha, string file)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" show {2}:\"{3}\"", path.Replace("\\", "/"), path.Replace("\\", "/"), sha, file));
            return output;
        }

        public static string ShowFileBeforeCommit(string path, string sha, string file)
        {
            var output = RunProcess(string.Format(" --git-dir=\"{0}/.git\" --work-tree=\"{1}\" show {2}~1:\"{3}\"", path.Replace("\\", "/"), path.Replace("\\", "/"), sha, file));
            return output;
        }
    }
}
