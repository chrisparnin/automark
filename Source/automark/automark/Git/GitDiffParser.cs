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
    public class GitDiffParser
    {
        public GitDiffParser()
        {
        }

        //public IEnumerable<HunkRangeInfo> Parse(string text)
        //{
        //    return from hunkLine in GetUnifiedFormatHunkLines(text)
        //           where !string.IsNullOrEmpty(hunkLine.Item1)
        //           select new HunkRangeInfo(new HunkRange(GetHunkOriginalFile(hunkLine.Item1)), new HunkRange(GetHunkNewFile(hunkLine.Item1)), hunkLine.Item2, hunkLine.Item3);
        //}
        public List<FileDiff> Parse(string text)
        {
            var list = new List<FileDiff>();

            foreach (var chunk in SplitFileHunks(text))
            {
                var split = chunk.Split('\n').AsEnumerable();

                var hunks = GetUnifiedFormatHunkLines(split)
                    .Where(line => !string.IsNullOrEmpty(line.Item1))
                    .Select(line => new HunkRangeInfo(
                                        new HunkRange(GetHunkOriginalFile(line.Item1)),
                                        new HunkRange(GetHunkNewFile(line.Item1)),
                                        line.Item2, GetFileName(split)
                                        )
                    ).ToList();
                list.Add(new FileDiff { Hunks = hunks });
            }

            foreach (var file in list)
            {
                if (file.Hunks.Any())
                {
                    file.FileName = file.Hunks.First().FileName;
                }
            }

            return list;
        }

        protected List<string> SplitFileHunks(string text)
        {
            var split = text.Split('\n').AsEnumerable();

            var parts = new List<string>();
            // skip initial header
            split = split.SkipWhile(s => !s.StartsWith("--- "));
            var fileHeader = split.Take(1);
            while (split.Any())
            {
                // create flat string containing all lines associated with a file.
                parts.Add(string.Join("\n", fileHeader.Concat(split.Skip(1).TakeWhile(s => !s.StartsWith("--- ")))));

                split = split.Skip(1).SkipWhile(s => !s.StartsWith("--- "));
                fileHeader = split.Take(1);
            }
            return parts;
        }


        public IEnumerable<Tuple<string, IEnumerable<string>>> GetUnifiedFormatHunkLines(IEnumerable<string> split)
        {
            var firstHunk = true;

            var hunkLine = "";
            var diffs = new List<string>();

            var withoutHeader = split.SkipWhile(s => !s.StartsWith("@@"));

            foreach (var line in withoutHeader)
            {
                if (line.StartsWith("@@"))
                {
                    if (firstHunk)
                    {
                        hunkLine = line.Trim();
                        firstHunk = false;
                    }
                    else
                    {
                        yield return new Tuple<string, IEnumerable<string>>(hunkLine, diffs);
                        hunkLine = line.Trim();
                        diffs.Clear();
                    }
                }
                else
                {
                    diffs.Add((line));
                }
            }

            yield return new Tuple<string, IEnumerable<string>>(hunkLine, diffs);
        }


        public string GetFileName(IEnumerable<string> lines)
        {
            var fileInfo = lines.SkipWhile(s => !s.StartsWith("--- "));
            var fileA = fileInfo.FirstOrDefault().Remove(0, 4);
            var fileB = fileInfo.Skip(1).FirstOrDefault().Remove(0, 4);
            if (fileA.StartsWith("a/"))
                return fileA.Remove(0, 2).Trim();
            if (fileB.StartsWith("b/"))
                return fileB.Remove(0, 2).Trim();
            return null;
        }

        public string GetHunkOriginalFile(string hunkLine)
        {
            return hunkLine.Split(new[] { "@@ -", " +" }, StringSplitOptions.RemoveEmptyEntries).First();
        }

        public string GetHunkNewFile(string hunkLine)
        {
            return hunkLine.Split(new[] { "@@ -", " +" }, StringSplitOptions.RemoveEmptyEntries).ToArray()[1].Split(' ')[0];
        }
    }
}
