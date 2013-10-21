using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            string path = (new System.Uri(Assembly.GetExecutingAssembly().CodeBase)).AbsolutePath;
            string directory = System.IO.Path.GetDirectoryName(path);
            string templatePath = System.IO.Path.Combine(directory, "Generate", "Export", "PostTemplate.html");

            if (File.Exists(templatePath))
            {
                var template = File.ReadAllText(templatePath);
                var res = template + "<body>" + html + "</body></html>";
                return res;
            }
            else
            { 
                return "<html><body>" + html + "</body></html>";
            }
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
