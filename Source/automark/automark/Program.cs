using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Connections.Browser;
using automark.Generate.Export;
using automark.Git;
using automark.Util;

namespace automark
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autogit");
            path = @"C:\dev\automark\Source\automark\.HistoryData\LocalHistory";
            if (args.Length > 0)
                path = args[0];
            var output = GitCommands.ListShaWithFiles(path);

            var parser = new ParseGitLog();
            var diffParser = new GitDiffParser();
            var commits = parser.Parse(output);

            Console.WriteLine(commits.Count);
            foreach (var commit in commits)
            {
                commit.UnifiedDiff = GitCommands.ShowSha(path, commit.Sha);

                // skip big files for arbiturary definition of big.
                if (commit.UnifiedDiff.Length > 500000)
                    continue;

                commit.Difflets = diffParser.Parse(commit.UnifiedDiff);

                foreach (var file in commit.Files)
                {
                    if( file.Status != "A" )
                        file.BeforeText = GitCommands.ShowFileBeforeCommit(path, commit.Sha, file.File);
                    if( file.Status != "D" )
                        file.AfterText = GitCommands.ShowFileAfterCommit(path, commit.Sha, file.File);
                }

                //commit.Print();
            }

            var connector = new ChromeHistory();

            //string dbPath = @"C:\Users\Chris\AppData\Local\Google\Chrome\User Data\Default\History";
            string dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\History");
            //string dbPath = @"\\psf\Home\Library\Application Support\Google\Chrome\Default\History"
            if (System.IO.File.Exists(dbPath))
            {
                // Chrome keeps an exclusive lock on database while open; copy-local
                var tempPath = "tempHistory.db";
                System.IO.File.Copy(dbPath, tempPath, true);

                var visits = connector.RecentStackoverflow(tempPath);
                var last = commits.FirstOrDefault();
                foreach (var commit in commits.Skip(1) )
                {
                    var lastTime = ParseGitLog.GetDateFromGitFormat(last.Headers["Date"]);
                    var commitTime = ParseGitLog.GetDateFromGitFormat( commit.Headers["Date"] );

                    commit.Visits.AddRange(visits.Where(v => v.Timestamp < lastTime && v.Timestamp >= commitTime));
                    last = commit;
                }

                // Clean up
                GC.Collect();
                connector = null;
                new System.Threading.Thread((db) =>
                {
                    System.Threading.Thread.Sleep(1000);
                    System.IO.File.Delete((string)db);
                }).Start(tempPath);
            }

            var formatter = new AsMarkdown();
            Console.WriteLine(formatter.Export(commits));

            //var html = new AsMarkdownHtml();
            //Console.WriteLine(html.Export(commits));

        }
    }
}