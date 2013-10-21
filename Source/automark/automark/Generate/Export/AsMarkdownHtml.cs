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
            //markdown2Html.EncodeProblemUrlCharacters = true;
            var html = markdown2Html.Transform(markdown);

            var template = File.ReadAllText("Generate/Export/PostTemplate.html");

            var res =  template + "<body>" + html + "</body></html>";
            return res;
            // Remove BOM that Visual Studio places in files.
            //return res.Replace("ï»¿", "");
            //return res.Replace("\uEFBBBF", "");
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
