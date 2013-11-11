using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using automark.Models;
using automark.Models.Diff;

namespace automark.Git
{

    public class UnifiedDiffToMyersDifference
    {
        const string REM_PREFIX = "-";
        const string ADD_PREFIX = "+";

        public static IEnumerable<Difference> DifferenceFromHunk(List<HunkRangeInfo> hunks)
        {
            foreach (var hunk in hunks)
            {
                int leftStart = 0;
                int rightStart = 0;
                int leftDiffStart = 0;
                int rightDiffStart = 0;
                int leftDiffEnd = 0;
                int rightDiffEnd = 0;
                int leftLineOffset = 0;
                int rightLineOffset = 0;

                int lineCounter = 0;

                string state = "NEW";
                var lines = hunk.DiffLines.AsEnumerable();


                while (state != "DONE")
                {
                    var line = lines.FirstOrDefault();
                    bool push = false;

                    if (state == "NEW")
                    {
                        if (line == null)
                        {
                            state = "DONE";
                        }
                        else if (line.StartsWith(REM_PREFIX) || line.StartsWith(ADD_PREFIX))
                        {
                            state = "DIFF_START";
                        }
                        else
                        {
                            push = true;
                            state = "NEW";
                        }
                    }

                    else if (state == "DIFF_START")
                    {
                        if (line.StartsWith(REM_PREFIX))
                        {
                            leftStart = leftLineOffset;
                            leftDiffStart = lineCounter;
                            leftDiffEnd = lineCounter;
                            state = "DIFF_MINUS";
                        }
                        else if (line.StartsWith(ADD_PREFIX))
                        {
                            rightStart = rightLineOffset;
                            rightDiffStart = lineCounter;
                            rightDiffEnd = lineCounter;
                            state = "DIFF_PLUS";
                        }
                        push = true;
                    }
                    else if (state == "DIFF_MINUS")
                    {
                        if (line == null)
                        {
                            yield return EmitRemove(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset,
                                       hunk);
                            state = "DONE";
                        }
                        else if (line.StartsWith(REM_PREFIX))
                        {
                            state = "DIFF_MINUS";
                            leftDiffEnd = lineCounter;
                            push = true;
                        }
                        else if (line.StartsWith(ADD_PREFIX))
                        {
                            rightStart = rightLineOffset;
                            rightDiffStart = lineCounter;
                            rightDiffEnd = lineCounter;

                            state = "DIFF_MOD";
                            push = true;
                        }
                        else
                        {
                            // EMIT DEL
                            yield return EmitRemove(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset,
                                       hunk);
                            state = "NEW";
                        }
                    }
                    else if (state == "DIFF_PLUS")
                    {
                        if (line == null)
                        {
                            yield return EmitNew(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset, rightDiffStart, rightDiffEnd,
                                       hunk);
                            state = "DONE";
                        }
                        else if (line.StartsWith(ADD_PREFIX))
                        {
                            state = "DIFF_PLUS";
                            rightDiffEnd = lineCounter;
                            push = true;
                        }
                        else
                        {
                            // EMIT ADD
                            yield return EmitNew(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset, rightDiffStart, rightDiffEnd,
                                       hunk);
                            state = "NEW";
                        }
                    }
                    else if (state == "DIFF_MOD")
                    {
                        if (line == null)
                        {
                            yield return EmitChange(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset, 
                                       leftDiffStart, leftDiffEnd, rightDiffStart, rightDiffEnd,
                                       hunk);
                            state = "DONE";
                        }
                        else if (line.StartsWith(ADD_PREFIX))
                        {
                            state = "DIFF_MOD";
                            rightDiffEnd = lineCounter;
                            push = true;
                        }
                        else
                        {
                            // EMIT Change
                            yield return EmitChange(leftStart, leftLineOffset,
                                       rightStart, rightLineOffset,
                                       leftDiffStart, leftDiffEnd, rightDiffStart, rightDiffEnd,
                                       hunk);
                            state = "NEW";
                        }
                    }

                    //// Push onto stack
                    if (push)
                    {
                        lines = IncrementLineState(lines, ref leftLineOffset, ref rightLineOffset, ref lineCounter);
                    }
                }
            }
        }

        public static IEnumerable<string> IncrementLineState(IEnumerable<string> lines, 
            ref int leftLineOffset, ref int rightLineOffset, ref int lineCounter)
        {
            var line = lines.Take(1).SingleOrDefault();
            if (line != null)
            {
                if (line.StartsWith(REM_PREFIX))
                {
                    leftLineOffset++;
                }
                else if (line.StartsWith(ADD_PREFIX))
                {
                    rightLineOffset++;
                }
                else
                {
                    leftLineOffset++;
                    rightLineOffset++;
                }

                lineCounter++;
            }

            return lines.Skip(1).ToList();
        }

        // Difference based on Visual Studio's Difference structure.
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.text.differencing.difference.aspx
        // A span can be 0-length:
        // http://msdn.microsoft.com/en-us/library/microsoft.visualstudio.text.span.aspx
        // This structure represents an immutable integer interval that describes a range of values, 
        // from Start to End. It is closed on the left and open on the right: [Start .. End). 

        public static Difference EmitNew(int leftStart, int leftEnd, int rightStart,int rightEnd,
            int rightDiffStart, int rightDiffEnd,
            HunkRangeInfo hunk)
        {
            return new Difference()
            {
                LeftStartingLineNumber = hunk.OriginalHunkRange.StartingLineNumber,
                RightStartingLineNumber = hunk.NewHunkRange.StartingLineNumber,
                DifferenceType = DifferenceType.Add,
                Left = new Span()
                {
                    Start = hunk.OriginalHunkRange.StartingLineNumber + leftStart,
                    Length = 0, // Zero-length
                    End = hunk.OriginalHunkRange.StartingLineNumber + leftEnd
                },
                Right = new Span()
                {
                    Start = rightStart + hunk.NewHunkRange.StartingLineNumber,
                    Length = rightEnd - rightStart,
                    End = rightEnd + hunk.NewHunkRange.StartingLineNumber,
                    TextLines = hunk.DiffLines.GetRange( rightDiffStart, rightDiffEnd - rightDiffStart + 1),
                    DiffStart = rightDiffStart,
                    DiffEnd = rightDiffEnd
                }
            };
        }

        public static Difference EmitChange(int leftStart, int leftEnd, int rightStart, int rightEnd, 
            int leftDiffStart, int leftDiffEnd, int rightDiffStart, int rightDiffEnd,
            HunkRangeInfo hunk)
        {
            var diff = new Difference()
            {

                LeftStartingLineNumber = hunk.OriginalHunkRange.StartingLineNumber,
                RightStartingLineNumber = hunk.NewHunkRange.StartingLineNumber,
                DifferenceType = DifferenceType.Change,
                Left = new Span()
                {
                    Start = hunk.OriginalHunkRange.StartingLineNumber + leftStart,
                    Length = leftEnd - leftStart,
                    End = hunk.OriginalHunkRange.StartingLineNumber + leftEnd,
                    TextLines = hunk.DiffLines.GetRange( leftDiffStart, leftDiffEnd - leftDiffStart + 1 ),
                    DiffStart = leftDiffStart,
                    DiffEnd = leftDiffEnd
                },
                Right = new Span()
                {
                    Start = rightStart + hunk.NewHunkRange.StartingLineNumber,
                    Length = rightEnd - rightStart,
                    End = rightEnd + hunk.NewHunkRange.StartingLineNumber,
                    TextLines = hunk.DiffLines.GetRange( rightDiffStart, rightDiffEnd - rightDiffStart + 1),
                    DiffStart = rightDiffStart,
                    DiffEnd = rightDiffEnd
                }
            };
            return diff;
        }

        public static Difference EmitRemove(int leftStart, int leftEnd, int rightStart, int rightEnd, HunkRangeInfo hunk)
        {
            return new Difference()
            {
                LeftStartingLineNumber = hunk.OriginalHunkRange.StartingLineNumber,
                RightStartingLineNumber = hunk.NewHunkRange.StartingLineNumber,
                DifferenceType = DifferenceType.Remove,
                Left = new Span()
                {
                    Start = hunk.OriginalHunkRange.StartingLineNumber + leftStart,
                    Length = leftEnd - leftStart,
                    End = hunk.OriginalHunkRange.StartingLineNumber + leftEnd
                },
                Right = new Span()
                {
                    Start = rightStart + hunk.NewHunkRange.StartingLineNumber,
                    Length = 0, // Zero-length
                    End = rightEnd + hunk.NewHunkRange.StartingLineNumber
                }
            };
        }

    }
}