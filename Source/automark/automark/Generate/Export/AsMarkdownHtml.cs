using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Models;
using MarkdownSharp;

namespace automark.Generate.Export
{
    public class AsMarkdownHtml
    {
        public string Export(List<GitCommit> commits)
        {
            AsMarkdown formatter = new AsMarkdown();
            var markdown = formatter.Export(commits);

            var markdown2Html = new Markdown();
            var html = markdown2Html.Transform(markdown);
            return html;
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
