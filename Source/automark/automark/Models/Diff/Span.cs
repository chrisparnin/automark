using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace automark.Models.Diff
{
    public struct Span
    {
        public int End { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public bool IsEmpty { get; set; }

        public List<string> TextLines { get; set; }
        public int DiffStart { get; set; }
        public int DiffEnd { get; set; }

        public bool Inside(Span other)
        {
            if (other.Start >= this.Start &&
                other.End <= this.End)
                return true;
            return false;
        }
    }
}
