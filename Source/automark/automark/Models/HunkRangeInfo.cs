using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace automark.Models
{
    public class HunkRangeInfo
    {
        public List<string> DiffLines { get; set; }
        public string FileName { get; set; }

        public HunkRangeInfo(HunkRange originaleHunkRange, HunkRange newHunkRange, IEnumerable<string> diffLines, string fileName)
        {
            OriginalHunkRange = originaleHunkRange;
            NewHunkRange = newHunkRange;

            // Don't want things like "git --diff, which are stuck on the bottom.  Discard anything after hunk-specified range:
            var lines = Math.Max(OriginalHunkRange.NumberOfLines, NewHunkRange.NumberOfLines);
            DiffLines = diffLines.Take(lines).ToList();
            this.FileName = fileName;

            IsAddition = DiffLines.All(s => s.StartsWith("+") || string.IsNullOrWhiteSpace(s));
            IsDeletion = DiffLines.All(s => s.StartsWith("-") || string.IsNullOrWhiteSpace(s));
            IsModification = !IsAddition && !IsDeletion;

            if (IsDeletion || IsModification)
            {
                OriginalText = DiffLines.Where(s => s.StartsWith("-")).Select(s => s.Remove(0, 1).TrimEnd('\n').TrimEnd('\r')).ToList();
            }
        }

        public HunkRange OriginalHunkRange { get; private set; }
        public HunkRange NewHunkRange { get; private set; }
        public List<string> OriginalText { get; private set; }

        public bool IsAddition { get; private set; }
        public bool IsModification { get; private set; }
        public bool IsDeletion { get; private set; }
    }
}
