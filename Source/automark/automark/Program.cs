using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Connections.Browser;
using automark.Generate.Export;
using automark.Git;
using automark.Models;
using automark.Util;

namespace automark
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autogit");
            //path = @"C:\dev\github\automark\Source\automark\.HistoryData\LocalHistory";
            //fatal: bad default revision 'HEAD'
            path = @"C:\Users\Chris\Downloads\HistoryData\.HistoryData\LocalHistory";
            var reverse = false;
            var html = false;
            if (args.Length > 0)
                path = args[0];
            if (args.Any(a => a == "-r"))
            {
                reverse = true;
            }
            if (args.Any(a => a == "-html"))
            {
                html = true;
            }

            var output = GitCommands.ListShaWithFiles(path);
            if (output == "")
            {
                Console.Error.WriteLine("There are no commits to report yet.");
            }

            var parser = new ParseGitLog();
            var diffParser = new GitDiffParser();
            var commits = parser.Parse(output);

            //Console.WriteLine(commits.Count);
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
            var firefox = new FirefoxConnector();

            //string dbPath = @"C:\Users\Chris\AppData\Local\Google\Chrome\User Data\Default\History";
            string dbPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Google\Chrome\User Data\Default\History");

            //string dbPath = @"\\psf\Home\Library\Application Support\Google\Chrome\Default\History"
            var visits = GetWebVisits(connector, dbPath, "chromeTemp.db");
           
            var fireFoxVisits = new List<WebVisit>();
            if( firefox.FindDbPath() != null )
                fireFoxVisits = GetWebVisits(firefox, firefox.FindDbPath(), "fireTemp.db");

            var last = commits.FirstOrDefault();
            foreach (var commit in commits.Skip(1))
            {
                var lastTime = ParseGitLog.GetDateFromGitFormat(last.Headers["Date"]);
                var commitTime = ParseGitLog.GetDateFromGitFormat(commit.Headers["Date"]);

                commit.Visits.AddRange(visits.Where(v => v.Timestamp < lastTime && v.Timestamp >= commitTime));
                commit.Visits.AddRange(fireFoxVisits.Where(v => v.Timestamp < lastTime && v.Timestamp >= commitTime));

                last = commit;
            }


            if (reverse)
            {
                commits.Reverse();
            }
            if (html)
            {
                var formatter = new AsMarkdownHtml();
                Console.WriteLine(formatter.Export(commits));
            }
            else 
            {
                var formatter = new AsMarkdown();
                Console.WriteLine(formatter.Export(commits));            
            }

            //var html = new AsMarkdownHtml();
            //Console.WriteLine(html.Export(commits));

        }

        private static List<WebVisit> GetWebVisits(SqlLiteConnector connector, string dbPath, string tempName)
        {
            var visits = new List<WebVisit>();
            if (System.IO.File.Exists(dbPath))
            {
                try
                {
                    // Chrome keeps an exclusive lock on database while open; copy-local
                    var tempPath = tempName;
                    System.IO.File.Copy(dbPath, tempPath, true);

                    visits = connector.RecentStackoverflow(tempPath);

                    // Clean up
                    GC.Collect();
                    connector = null;
                    new System.Threading.Thread((db) =>
                    {
                        System.Threading.Thread.Sleep(1000);
                        System.IO.File.Delete((string)db);
                    }).Start(tempPath);
                }
                catch (Exception ex)
                {
                    Trace.Write(ex.Message);
                }
            }
            return visits;
        }
    }
}