using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace automark.Models.Markdown
{
    public class FileChangeHunks
    {
        public string File { get; set; }

        public List<ChangeHunk> Changes { get; set; }
    }
}
