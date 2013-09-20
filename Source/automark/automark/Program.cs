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
            string path = @"C:\Users\Chris\AppData\Roaming\autogit";
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

            string dbPath = @"C:\Users\Chris\AppData\Local\Google\Chrome\User Data\Default\History";
            //string dbPath = @"\\psf\Home\Library\Application Support\Google\Chrome\Default\History"
            // TODO: database is locked.
            if (System.IO.File.Exists(dbPath))
            {
                var visits = connector.RecentStackoverflow(dbPath);
                var previous = commits.FirstOrDefault();
                foreach (var commit in commits.Skip(1) )
                {
                    var previousTime = ParseGitLog.GetDateFromGitFormat( previous.Headers["Date"] );
                    var commitTime = ParseGitLog.GetDateFromGitFormat( commit.Headers["Date"] );

                    commit.Visits.AddRange( visits.Where( v => v.Timestamp > previousTime && v.Timestamp <= commitTime ) );
                    previous = commit;
                }
            }

            var formatter = new AsMarkdown();
            Console.WriteLine(formatter.Export(commits));

            //var html = new AsMarkdownHtml();
            //Console.WriteLine(html.Export(commits));

        }
    }
}