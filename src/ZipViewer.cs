using System.IO.Compression;
using System.Text;
using SixLabors.ImageSharp;

/// <summary>
/// <see cref="ZipArchiveEntry" /> wrapper
/// </summary>
internal class ZipEntry(ZipArchiveEntry zipEntry) : IEntry<ZipArchiveEntry>
{
    public ZipArchiveEntry Entry => zipEntry;

    public string Name => zipEntry.FullName;

    public Stream Open()
    {
        return Entry.Open();
    }
}

/// <summary>
/// ImageViewer for Zip
/// </summary>
internal class ZipViewer : ImageViewer<ZipEntry>
{
    public ZipViewer(FileInfo file, PageMode pageMode = PageMode.LeftToRight)
    {
        PageMode = pageMode;
        Zip = new(file.OpenRead(), ZipArchiveMode.Read, false, Encoding.UTF8);
        Entries = Zip.Entries.AsParallel()
                             .Where(EntryFilter)
                             .OrderBy(static entry => entry.FullName)
                             .Select(static entry => new ZipEntry(entry))
                             .ToArray();
    }
    private ZipArchive Zip { get; }

    private static bool EntryFilter(ZipArchiveEntry entry)
    {
        if (entry.IsEncrypted)
            return false;
        if (string.IsNullOrEmpty(entry.Name))
            return false;
        return IsImageFile(Path.GetExtension(entry.Name));
    }

    private bool disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Entries = [];
                Zip.Dispose();
            }

            disposedValue = true;
        }
    }
}
