using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using automark.Models;
using automark.Models.Diff;

namespace automark.Transformations.Rewrite
{
    class MergeFixOnFix
    {
        // If previous block is add and next block is change, and both overlap, then merge.
        public FileDiff Apply(FileDiff previous, FileDiff next)
        {
            var newBlock = new FileDiff();
            if (previous == null || next == null)
                return null;
            if (previous.FileName != next.FileName)
                return null;

            try
            {
                // Let's keep simple case for now, just one hunk, with an equal sized mod
                if (previous.Hunks.Count == next.Hunks.Count && previous.Hunks.Count == 1 &&
                    next.MyerDiffs[0].Left.Length == next.MyerDiffs[0].Right.Length
                    )
                {
                    if (previous.MyerDiffs.All(m => m.DifferenceType == Models.Diff.DifferenceType.Add) &&
                        next.MyerDiffs.All(m => m.DifferenceType == Models.Diff.DifferenceType.Change)
                       )
                    {
                        //if (previous.MyerDiffs[0].Left.Inside(next.MyerDiffs[0].Left) &&
                        //    next.MyerDiffs[0].Right.Inside(next.MyerDiffs[0].Right))
                        if (next.MyerDiffs[0].Left.TextLines.All(t => previous.MyerDiffs[0].Right.TextLines.Contains("+" + t.Remove(0,1))))
                        {
                            newBlock.Hunks = previous.Hunks;
                            newBlock.MyerDiffs = previous.MyerDiffs;

                            for (int i = 0; i < next.MyerDiffs[0].Right.TextLines.Count; i++)
                            {
                                var fixOnFixLine = next.MyerDiffs[0].Right.TextLines[i];
                                var prevLine = next.MyerDiffs[0].Left.Start - previous.Hunks[0].NewHunkRange.StartingLineNumber + i;

                                newBlock.Hunks[0].DiffLines[prevLine] = fixOnFixLine;
                            }
                            return newBlock;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            return null;
        }
    }
}
