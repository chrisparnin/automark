using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Models;

namespace automark.Git
{
    class ParseGitLog
    {
        public static DateTime GetDateFromGitFormat(string dateStr)
        {
            string format = "ddd MMM d HH:mm:ss yyyy zzz";
            return DateTimeOffset.ParseExact(dateStr, format, CultureInfo.InvariantCulture).DateTime;
        }

        public List<GitCommit> Parse(string output)
        {
            GitCommit commit = null;
            var commits = new List<GitCommit>();
            bool processingMessage = false;
            using (var strReader = new StringReader(output))
            {
                do
                {
                    var line = strReader.ReadLine();
                    if (line == null)
                        break;

                    if( line.StartsWith("commit ") )
                    {
                        if (commit != null)
                            commits.Add(commit);
                        commit = new GitCommit();
                        commit.Sha = line.Split(' ')[1];
                    }

                    if ( StartsWithHeader(line) )
                    {
                        var header = line.Split(':')[0];
                        var val = string.Join(":",line.Split(':').Skip(1)).Trim();

                        // headers
                        commit.Headers.Add(header, val);
                    }

                    if (string.IsNullOrEmpty(line) )
                    {
                        // commit message divider
                        processingMessage = !processingMessage;
                    }

                    if (line.Length > 0 && line[0] == '\t' || 
                       (line.Length > 4 && line.Substring(0,4).All( ch => ch == ' ') ) )
                    { 
                        // commit message.
                        commit.Message += line;
                    }

                    if (line.Length > 1 && Char.IsLetter(line[0]) && line[1] == '\t')
                    {
                        var status = line.Split('\t')[0];
                        var file = line.Split('\t')[1];
                        commit.Files.Add(new GitFileStatus() { Status = status, File = file } );
                    }
                }
                while (strReader.Peek() != -1);
            }
            if (commit != null)
                commits.Add(commit);

            return commits;
        }

        private bool StartsWithHeader(string line)
        {
            if( line.Length > 0 && char.IsLetter( line[0] ) )
            {
                var seq = line.SkipWhile( ch => Char.IsLetter(ch) && ch != ':' );
                return seq.FirstOrDefault() == ':';
            }
            return false;
        }
    }
}
