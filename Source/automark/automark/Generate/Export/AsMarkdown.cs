using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using automark.Git;
using automark.Models;

namespace automark.Generate.Export
{
    public class AsMarkdown
    {
        public string Export(List<GitCommit> commits, bool debug)
        {
            if (!commits.Any())
                return "";

            StringWriter w = new StringWriter();

            if( commits.Any() )
            {
                w.WriteLine(EmitDate(commits.First().CommitTimeStamp));
            }

            var diffEngine = new DiffMatchPatch.diff_match_patch();

            GitCommit previousCommit = null;

            bool isFirstCommit = true;
            foreach (var commit in commits)
            {
                var span = TimeSinceLastCommit(commit, previousCommit);

                if (span != TimeSpan.MaxValue && !IsCommitOnSameDay(commit, previousCommit))
                {
                    w.WriteLine(EmitDate(commit.CommitTimeStamp));
                }

                if (isFirstCommit || (span != TimeSpan.MaxValue && span.TotalHours > 2) || !IsCommitOnSameDay(commit, previousCommit))
                {
                    w.WriteLine("");
                    w.WriteLine(string.Format("<div class='section'>{0}<div></div><div class='summary'></div></div>", EmitTime(commit.CommitTimeStamp)));
                    isFirstCommit = false;
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
                    // test space
                    if (debug) 
                    {
                        w.WriteLine("##### Myers Version");
                        var diffs = fileDiff.MyerDiffs;
                        foreach (var diff in diffs)
                        {
                            w.WriteLine("Type {0} Left {1}:{2} Right {3}:{4}", diff.DifferenceType,
                                diff.Left.Start, diff.Left.End, diff.Right.Start, diff.Right.End);

                            if (diff.DifferenceType == Models.Diff.DifferenceType.Change &&
                                diff.Left.Length == diff.Right.Length)
                            {
                                w.WriteLine();
                                for (var i = 0; i < diff.Left.Length; i++)
                                {
                                    var left = diff.Left.TextLines[i];
                                    var right = diff.Right.TextLines[i];

                                    var innerDiffs = diffEngine.diff_main(left, right, false);
                                    diffEngine.Diff_Timeout = 0;
                                    diffEngine.diff_cleanupSemantic(innerDiffs);

                                    w.WriteLine(string.Join(",", innerDiffs.Where(d => d.operation != DiffMatchPatch.Operation.EQUAL).Select(d => d.text)));
                                }
                            }
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
                return string.Format("{0:HH:mm}", date);
            return string.Format("{0:hh:mm tt}", date);
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

        public bool IsCommitOnSameDay(GitCommit current, GitCommit previous)
        {
            if (previous == null || current == null)
                return true;
            var currentDate = current.CommitTimeStamp;
            var previousDate = previous.CommitTimeStamp;

            return currentDate.DayOfWeek == previousDate.DayOfWeek;
        }

        public void ExportToFile(List<GitCommit> commits, string outputPath)
        {
            using (TextWriter writer = File.CreateText(outputPath))
            {
                writer.Write(Export(commits, false));
            }
        }
    }
}
