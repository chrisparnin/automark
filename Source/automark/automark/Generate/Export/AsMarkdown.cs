using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using automark.Models;

namespace automark.Generate.Export
{
    public class AsMarkdown
    {
        public string Export(List<GitCommit> commits)
        {
            if (!commits.Any())
                return "";

            StringWriter w = new StringWriter();

            if( commits.Any() )
            {
                w.WriteLine(EmitDate(commits.First().CommitTimeStamp));
            }

            GitCommit previousCommit = null;
            foreach (var commit in commits)
            {
                var span = TimeSinceLastCommit(commit, previousCommit);
                // commit for files not yet in repository, skip.
                // in future can be smarter with this with tags, etc.
                if (commit.Message.Contains("pre save"))
                    continue;

                if (span != TimeSpan.MaxValue && span.TotalDays >= 1)
                {
                    w.WriteLine(EmitDate(commit.CommitTimeStamp));
                }
                // Sunday, October 6 2013
                // 10:44 AM
                //if (span != TimeSpan.MaxValue && span.TotalHours > 2)
                //{
                //    w.WriteLine("<div class='divider'></div>");
                //    w.WriteLine(EmitTime(commit.CommitTimeStamp));                    
                //}

                if( commit.Visits.Any() )
                {
                    //w.WriteLine("#### Visited ");
                    w.WriteLine("   ");
                    foreach (var visit in commit.Visits)
                    {
                        w.WriteLine("* [{0}]({1})",
                            string.IsNullOrEmpty(visit.Title) ? visit.Url : visit.Title,
                            visit.Url);
                    }
                    w.WriteLine("   ");
                    if (commit.Visits.Count == 1)
                    {
                        w.WriteLine("<div></div>");
                    }
                }

                foreach (var fileDiff in commit.Difflets)
                {
                    //w.WriteLine("> {0} to {1}", file.Status, file.File);
                    w.WriteLine("#### {0}", fileDiff.FileName);
                    w.WriteLine();
                    foreach (var hunk in fileDiff.Hunks)
                    {
                        foreach (var line in hunk.DiffLines)
                        {
                            // http://stackoverflow.com/questions/8301207/microsoft-ides-source-file-encodings-boms-and-the-unicode-character-ufeff
                            // default filter: skip context of adding using.
                            if (line.Trim().StartsWith("using ") )
                                continue;

                            w.WriteLine("    {0}", line.TrimEnd() );
                        }
                    }
                }

                previousCommit = commit;

            }
            return w.ToString();
        }

        // TODO possible to introduce navigation helper, sidebar.?
        // TODO better summary/header the amount of commits, date range.
        // TODO collapsible sections (via javascript).
        private string EmitTime(DateTime date)
        {
            //Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("sv-SE");
            //Thread.CurrentThread.CurrentUICulture = new CultureInfo("sv-SE");

            if (System.Globalization.CultureInfo.CurrentUICulture.Name == "sv-SE")
                return string.Format("### {0:HH:mm}</p>", date);
            return string.Format("### {0:hh:mm tt}</p>", date);
        }

        private string EmitDate(DateTime date)
        {
            return string.Format("## {0:dddd, MMMM dd, yyyy}", date);
        }

        public TimeSpan TimeSinceLastCommit(GitCommit current, GitCommit previous)
        {
            if (previous == null || current == null)
                return TimeSpan.MaxValue;
            var currentDate = current.CommitTimeStamp;
            var previousDate = previous.CommitTimeStamp;

            return (currentDate - previousDate).Duration();
        }


        public void ExportToFile(List<GitCommit> commits, string outputPath)
        {
            using (TextWriter writer = File.CreateText(outputPath))
            {
                writer.Write(Export(commits));
            }
        }
    }
}
