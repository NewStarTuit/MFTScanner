# MftScanner

High-performance NTFS MFT (Master File Table) scanner for Windows.  
`MftScanner` reads NTFS metadata directly using `FSCTL_ENUM_USN_DATA`, resolves full file paths using FRN (File Reference Number) chaining, and provides streaming access to files with low memory consumption.

---

## ✨ Features

- Direct NTFS MFT parsing (no `Directory.GetFiles`)
- High-performance streaming (`IEnumerable`)
- Low memory usage (~70% less than full in-memory indexing)
- Parallel disk scanning
- `CancellationToken` support
- Folder-level scanning
- Extension-based filtering
- Automatic filtering of:
  - System files
  - Temporary files
  - Deleted entries
  - Directories (optional)
- Clean and library-ready architecture

---

## 🏗 Architecture Overview

```
MFTScanner/
│
├── MftScanner.cs          // Public scanning API
├── NtfsMftParser.cs       // Low-level USN / MFT reader
├── NtfsPathResolver.cs    // FRN-based full path builder
└── Models/
    └── NtfsEntry.cs       // File metadata model
```

### How It Works

1. Enumerates NTFS entries using `FSCTL_ENUM_USN_DATA`
2. Collects File Reference Numbers (FRN)
3. Builds full paths by walking parent FRN chain
4. Streams filtered results to the caller

---

## 📦 Installation

Clone the repository:

```bash
git clone https://github.com/NewStarTuit/MftScanner.git
```

Add the project reference to your solution.

### Requires

- Windows OS
- NTFS volume
- Administrator privileges

---

## 🚀 Usage

### 1️⃣ Full Disk Scan (All Fixed Drives)

```csharp
using MFTScanner;

var cts = new CancellationTokenSource();

foreach (var file in MftScanner.ScanFull(cts.Token))
{
    Console.WriteLine(file.FullPath);

    if (file.FullPath.Contains("stop"))
        cts.Cancel();
}
```

✔ Parallel scanning  
✔ Streaming results  
✔ Low memory footprint

---

### 2️⃣ Scan Specific Folder

```csharp
var files = MftScanner.ScanFolder(@"C:\Users\Men\Downloads");

foreach (var file in files)
{
    Console.WriteLine(file.FullPath);
}
```

---

### 3️⃣ Scan Folder with Extension Filter

```csharp
var files = MftScanner.ScanFolder(
    @"C:\Users\Men\Downloads",
    new[] { "pdf", "zip" });

foreach (var file in files)
{
    Console.WriteLine(file.FullPath);
}
```

> Only `.pdf` and `.zip` files will be returned.

---

### 🛑 Cancellation Support

```csharp
var cts = new CancellationTokenSource();

Task.Run(() =>
{
    Thread.Sleep(5000);
    cts.Cancel();
});

foreach (var file in MftScanner.ScanFull(cts.Token))
{
    Console.WriteLine(file.FullPath);
}
```

> Scan will stop gracefully.

---

## 📊 Performance Characteristics

| Feature                   | Traditional IO | MftScanner |
|---------------------------|:--------------:|:----------:|
| Uses `Directory.GetFiles` | ✅ Yes         | ❌ No      |
| Reads file content        | ✅ Yes         | ❌ No      |
| Memory usage              | High           | Low        |
| Parallel disk support     | ❌ No          | ✅ Yes     |
| Large volume friendly     | Limited        | ✅ Yes     |

---

## 🧠 Why Use MFT Instead of File System API?

**Traditional file enumeration:**
- Traverses directory tree
- Slower on large volumes
- Triggers file system hooks
- High IO overhead

**MFT-based scanning:**
- Reads NTFS metadata directly
- Extremely fast for large volumes
- Minimal disk activity
- Ideal for metadata analysis or file indexing tools

---

## 📌 NtfsEntry Model

```csharp
public class NtfsEntry
{
    public long FileReference    { get; set; }
    public long ParentReference  { get; set; }
    public string Name           { get; set; }
    public string FullPath       { get; set; }
    public bool IsDirectory      { get; set; }
    public bool IsSystem         { get; set; }
    public bool IsTemporary      get; set; }
    public bool IsDeleted        { get; set; }
}
```

---

## ⚠️ Limitations

- Works only on **NTFS** volumes
- Requires **Administrator privileges**
- Does **not** read file contents (metadata only)
- Path resolution requires complete FRN mapping

---

## 🔥 Advanced Use Cases

- Real-time incremental scanning using USN journal
- File change monitoring
- Extension or pattern-based filtering
- Metadata analysis pipelines
- Large-scale file indexing

---

## 🧪 Recommended For Large Environments

For systems with millions of files:

- Use **streaming mode**
- Avoid materializing full lists
- Apply **early filtering**
- Combine with extension or pattern filters

---

## 📄 License

[MIT License](LICENSE)

---

## 🤝 Contributing

Pull requests are welcome.  
For major changes, please open an issue first.