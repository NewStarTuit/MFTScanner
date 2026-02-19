using MFTReader;
using MFTScanner.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MFTScanner
{
    public static class MftScanner
    {
        // 🔥 FULL SCAN - Parallel + Streaming
        public static IEnumerable<NtfsEntry> ScanFull(
            CancellationToken token = default)
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.DriveType == DriveType.Fixed && d.IsReady)
                .Select(d => d.Name.TrimEnd('\\'))
                .ToList();

            return ScanDrivesInternal(drives, null, null, token);
        }

        // 🔥 FOLDER SCAN - Streaming
        public static IEnumerable<NtfsEntry> ScanFolder(
            string folderPath,
            string[] extensions = null,
            CancellationToken token = default)
        {
            folderPath = folderPath.TrimEnd('\\');
            string drive = Path.GetPathRoot(folderPath).TrimEnd('\\');

            var extSet = extensions != null && extensions.Length > 0
                ? new HashSet<string>(
                    extensions.Select(e => "." + e.ToLower()))
                : null;

            return ScanDrivesInternal(
                new List<string> { drive },
                folderPath,
                extSet,
                token);
        }

        private static IEnumerable<NtfsEntry> ScanDrivesInternal(
            List<string> drives,
            string folderFilter,
            HashSet<string> extFilter,
            CancellationToken token)
        {
            var collection = new BlockingCollection<NtfsEntry>(10000);

            Task.Run(() =>
            {
                Parallel.ForEach(drives, drive =>
                {
                    foreach (var entry in ScanDrive(drive, token))
                    {
                        if (token.IsCancellationRequested)
                            break;

                        if (folderFilter != null &&
                            !entry.FullPath.StartsWith(folderFilter,
                                StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (extFilter != null &&
                            !extFilter.Contains(
                                Path.GetExtension(entry.FullPath)
                                    .ToLower()))
                            continue;

                        collection.Add(entry, token);
                    }
                });

                collection.CompleteAdding();
            }, token);

            foreach (var item in collection.GetConsumingEnumerable(token))
                yield return item;
        }

        // 🔥 DRIVE SCAN (Streaming)
        private static IEnumerable<NtfsEntry> ScanDrive(
     string drive,
     CancellationToken token)
        {
            var allEntries = new List<NtfsEntry>();

            foreach (var entry in NtfsMftParser.EnumerateVolume(drive))
            {
                if (token.IsCancellationRequested)
                    yield break;

                allEntries.Add(entry);
            }

            // 🔥 Avval resolve
            NtfsPathResolver.ResolvePaths(allEntries, drive);

            // 🔥 Keyin filter
            foreach (var entry in allEntries)
            {
                if (token.IsCancellationRequested)
                    yield break;

                if (entry.IsDirectory ||
                    entry.IsSystem ||
                    entry.IsTemporary ||
                    entry.IsDeleted)
                    continue;

                yield return entry;
            }
        }

    }
}
