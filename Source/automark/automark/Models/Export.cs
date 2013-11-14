using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace automark.Models
{
    public class Export
    {
        public string ListShas { get; set; }
        public List<string> UnifiedDiffs { get; set; }
        public string UsageLog { get; set; }
        public List<string> MarkdownFiles { get; set; }
        public List<string> GeneratedHtmlFiles { get; set; }

        public string Error { get; set; }
    }
}
