using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace automark.Models.Diff
{
    public class Difference
    {
        public Span Left { get; set; }
        public Span Right { get; set; }
        public Match After { get; set; }
        public Match Before { get; set; }
        public DifferenceType DifferenceType { get; set; }

        public int LeftStartingLineNumber { get; set; }

        public int RightStartingLineNumber { get; set; }
    }

    public enum DifferenceType
    {
        Add,
        Remove,
        Change
    }
}
