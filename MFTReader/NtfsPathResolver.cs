using MFTScanner.Models;
using System.Collections.Generic;
using static MFTReader.NtfsMftParser;

namespace MFTScanner
{
    internal static class NtfsPathResolver
    {
        public static void ResolvePaths(List<NtfsEntry> entries, string drive)
        {
            var dict = new Dictionary<long, NtfsEntry>();

            foreach (var e in entries)
                dict[e.FileReference] = e;

            foreach (var e in entries)
                e.FullPath = BuildPath(e, dict, drive);
        }

        private static string BuildPath(
            NtfsEntry entry,
            Dictionary<long, NtfsEntry> dict,
            string drive)
        {
            var parts = new Stack<string>();
            var current = entry;

            while (current != null)
            {
                parts.Push(current.Name);

                if (!dict.TryGetValue(current.ParentReference, out current))
                    break;

                if (current.ParentReference == current.FileReference)
                    break;
            }

            return drive + "\\" + string.Join("\\", parts);
        }
    }
}
