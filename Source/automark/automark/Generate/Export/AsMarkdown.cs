using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
            foreach (var commit in commits)
            {
                // commit for files not yet in repository, skip.
                // in future can be smarter with this with tags, etc.
                if (commit.Message.Contains("pre save"))
                    continue;

                if( commit.Visits.Any() )
                {
                    w.WriteLine("### Visited ");
                    foreach (var visit in commit.Visits)
                    {
                        w.WriteLine("* [{0}]({1})",
                            string.IsNullOrEmpty(visit.Title) ? visit.Url : visit.Title,
                            visit.Url);
                    }
                }

                foreach (var fileDiff in commit.Difflets)
                {
                    //w.WriteLine("> {0} to {1}", file.Status, file.File);
                    w.WriteLine("### {0}", fileDiff.FileName);
                    w.WriteLine();
                    foreach (var hunk in fileDiff.Hunks)
                    {
                        foreach (var line in hunk.DiffLines)
                        {
                            w.WriteLine("     {0}", line.TrimEnd());
                        }
                    }
                }
            }
            return w.ToString();
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
