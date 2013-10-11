// Guids.cs
// MUST match guids.h
using System;

namespace ninlabs.automark.VisualStudio
{
    static class GuidList
    {
        public const string guidautomarkVisualStudioPkgString = "bbd85006-06ee-41de-8b7f-91d3ccb25658";
        public const string guidautomarkVisualStudioCmdSetString = "3872d00d-005f-4f7e-b9f9-ce3f36a3a82e";
        public const string guidToolWindowPersistanceString = "a09de029-ab47-4f1c-89c2-bb47f2349a6c";

        public static readonly Guid guidautomarkVisualStudioCmdSet = new Guid(guidautomarkVisualStudioCmdSetString);
    };
}