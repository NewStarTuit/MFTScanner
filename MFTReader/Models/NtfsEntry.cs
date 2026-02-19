namespace MFTScanner.Models
{
    public class NtfsEntry
    {
        public long FileReference { get; set; }
        public long ParentReference { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsDirectory { get; set; }
        public string FullPath { get; set; }
        public bool IsSystem { get; set; }
        public bool IsTemporary { get; set; }
    }
}
