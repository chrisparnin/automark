using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using automark.Connections.Browser;
using automark.Generate.Export;
using automark.Git;
using automark.Models;
using automark.Transformations.Rewrite;
using automark.Util;

namespace automark
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autogit");
            path = @"C:\DEV\github\automark\Source\Extensions\automark.VisualStudio\.HistoryData\LocalHistory";
            //path = @"C:\dev\github\automark\Source\automark\.HistoryData\LocalHistory";
            //fatal: bad default revision 'HEAD'
            //path = @"C:\Users\Chris\Downloads\HistoryData\.HistoryData\LocalHistory";
            var reverse = false;
            var html = false;
            var fuzz = false;
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
            if (args.Any(a => a == "-fuzz"))
            {
                fuzz = true;
            }

            var output = GitCommands.ListShaWithFiles(path);
            if (output == "")
            {
                Console.Error.WriteLine("There are no commits to report yet.");
            }

            var parser = new ParseGitLog();
            var diffParser = new GitDiffParser();
            var commits = parser.Parse(output);

            // commit for files not yet in repository, skip.
            // in future can be smarter with this with tags, etc.
            commits = commits.Where(c => !c.Message.Contains("pre save")).ToList();

            foreach (var commit in commits)
            {
                commit.UnifiedDiff = GitCommands.ShowSha(path, commit.Sha);

                // skip big files for arbiturary definition of big.
                if (commit.UnifiedDiff.Length > 500000)
                    continue;

                ParseUnifiedDiff(path, diffParser, commit);

                //commit.Print();
            }

            // Temporal fuzz
            if( fuzz )
            {
                var commitsToPrune = new List<GitCommit>();
                // Do processing of commits in order, just easier on the brain...
                var inOrderCommits = commits.ToList();
                inOrderCommits.Reverse();

                var prevCommit = inOrderCommits.FirstOrDefault();
                var accumalatedDifference = new TimeSpan();
                var startOfFuzz = prevCommit;
                var endOfFuzz = prevCommit;
                foreach (var commit in inOrderCommits.Skip(1))
                {
                    var lastTime = ParseGitLog.GetDateFromGitFormat(prevCommit.Headers["Date"]);
                    var commitTime = ParseGitLog.GetDateFromGitFormat(commit.Headers["Date"]);
                    var span = (lastTime - commitTime).Duration();
                    accumalatedDifference += span;
                    if (accumalatedDifference.TotalMinutes <= 3 && 
                        prevCommit.Files.All(f => commit.Files.Select(c => c.File).Contains(f.File)) && 
                        prevCommit.Files.Any( f => f.Status != "A" || f.Status != "D" ) )
                    {
                        commitsToPrune.Add(prevCommit);
                        endOfFuzz = commit;
                    }
                    else
                    {
                        // endOfFuzz will be only surviving commit in range, others will be pruned.  
                        // Get a new unified diff, and then reparse.
                        if (startOfFuzz != endOfFuzz)
                        {
                            try
                            {
                                endOfFuzz.UnifiedDiff = GitCommands.ShowDiffRange(path, startOfFuzz.Sha + "~1", endOfFuzz.Sha);
                                ParseUnifiedDiff(path, diffParser, endOfFuzz);
                            }
                            catch (Exception ex)
                            {
                                Trace.WriteLine(ex.Message);
                            }
                        }
                        accumalatedDifference = new TimeSpan();
                        startOfFuzz = commit;
                        endOfFuzz = commit;
                    }

                    prevCommit = commit;
                }

                if (startOfFuzz != endOfFuzz)
                {
                    try
                    {
                        endOfFuzz.UnifiedDiff = GitCommands.ShowDiffRange(path, startOfFuzz.Sha + "~1", endOfFuzz.Sha);
                        ParseUnifiedDiff(path, diffParser, endOfFuzz);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine(ex.Message);
                    }
                }

                foreach (var commitToRemove in commitsToPrune)
                {
                    commits.Remove(commitToRemove);
                }
            }

            //////////////////
            // CUSTOM FILTERS
            //////////////////
            commits = commits.Where(c => c.Difflets.Count > 0 && !c.Difflets[0].FileName.EndsWith(".csproj")).ToList();

            // Remove hunks that are only from newline.
            foreach (var commit in commits)
            {
                foreach (var fileDiff in commit.Difflets)
                {
                    fileDiff.Hunks = fileDiff.Hunks.Where( hunk =>
                        !(hunk.DiffLines
                            .Where(l => l.Trim().StartsWith("+"))
                            .All(l => l.Trim() == "+") && hunk.IsAddition)
                        &&
                        !(hunk.DiffLines
                            .Where(l => l.Trim().StartsWith("-"))
                            .All(l => l.Trim() == "-") && hunk.IsDeletion)
                    ).ToList();
                }
            }
            // Remove commits that now have 0 hunks.
            commits = commits.Where(c => c.Difflets.All(f => f.Hunks.Count > 0)).ToList();

            // Transformations
            var newCommits = new List<GitCommit>();

            try
            {
                var fixOnFix = new MergeFixOnFix();
                var lastCommit = commits.FirstOrDefault();

                foreach (var commit in commits.Skip(1))
                {
                    if( commit.Difflets.Count > 0 && lastCommit.Difflets.Count > 0 )
                    {
                        var newBlock = fixOnFix.Apply(commit.Difflets[0], lastCommit.Difflets[0]);
                        if (newBlock != null)
                        {
                            commit.Difflets[0] = newBlock;
                        }
                        else
                        {
                            newCommits.Add(lastCommit);
                        }
                    }
                    else
                    {
                        newCommits.Add(lastCommit);
                    }
                    lastCommit = commit;
                }
                if (lastCommit != null)
                {
                    newCommits.Add(lastCommit);
                }

                commits = newCommits;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message); // test fix on fix
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
                Console.WriteLine(formatter.Export(commits, args.Length == 0));            
            }

            //var html = new AsMarkdownHtml();
            //Console.WriteLine(html.Export(commits));
            if (args.Length == 0)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("sv-SE");
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");

                Console.WriteLine(string.Format("## {0:dddd, MMMM dd, yyyy}\u00e5", DateTime.Now.AddDays(-2)));

                Console.ReadKey();
            }
        }

        private static void ParseUnifiedDiff(string path, GitDiffParser diffParser, GitCommit commit)
        {
            commit.Difflets = diffParser.Parse(commit.UnifiedDiff);

            foreach (var file in commit.Files)
            {
                if (file.Status != "A")
                    file.BeforeText = GitCommands.ShowFileBeforeCommit(path, commit.Sha, file.File);
                if (file.Status != "D")
                    file.AfterText = GitCommands.ShowFileAfterCommit(path, commit.Sha, file.File);
            }
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