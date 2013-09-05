using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Git;
using automark.Util;

namespace automark
{
    class Program
    {
        static void Main(string[] args)
        {
            string path = @"C:\DEV\github\Codegrams";
            if (args.Length > 0)
                path = args[0];
            var output = GitCommands.ListShaWithFiles(path);

            var parser = new ParseGitLog();
            var commits = parser.Parse(output);

            Console.WriteLine(commits.Count);
            foreach (var commit in commits)
            {
                commit.Print();
            }


        }
    }
}