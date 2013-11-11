using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace automark.Transformations.Rewrite
{
    public class MergeTemporalFuzz
    {
        // First pass rearrange and mark blocks to be same files together if time is < delta
        // Next pass merge together files if changes overlap or if hunk is very small.
        // Future: Revert moves if not merged?

        // TODO: 1) move out myers diff calc. 2) Temporal reordering happens before so can merge hunks. 
    }
}
