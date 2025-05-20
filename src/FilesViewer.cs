/// <summary>
/// <see cref="FileInfo"/> wrapper
/// </summary>
internal class FileEntry(FileInfo fileEntry) : IEntry<FileInfo>
{
    public FileInfo Entry => fileEntry;

    public string Name => fileEntry.Name;

    public Stream Open()
    {
        return Entry.OpenRead();
    }
}

/// <summary>
/// ImageViewer for specified files or files in directory.
/// </summary>
internal class FilesViewer : ImageViewer<FileEntry>
{
    /// <summary>
    /// Constructor for the specified files
    /// </summary>
    public FilesViewer(IEnumerable<FileInfo> files, PageMode pageMode = PageMode.LeftToRight)
    {
        PageMode = pageMode;
        Entries = files.AsParallel()
                       .Where(EntryFilter)
                       .OrderBy(static f => f.Name)
                       .Select(static f => new FileEntry(f))
                       .ToArray();
    }

    /// <summary>
    /// Constructor for the directory
    /// </summary>
    public FilesViewer(DirectoryInfo dir, PageMode pageMode = PageMode.LeftToRight)
    {
        if (!dir.Exists)
            throw new DirectoryNotFoundException($"No such directory: {dir.FullName}");

        PageMode = pageMode;
        Entries = dir.EnumerateFiles()
                     .AsParallel()
                     .Where(EntryFilter)
                     .OrderBy(static f => f.Name)
                     .Select(static f => new FileEntry(f))
                     .ToArray();
    }

    private static bool EntryFilter(FileInfo entry)
    {
        if (!entry.Exists)
            return false;
        if (entry.Attributes.HasFlag(FileAttributes.Directory))
            return false;

        return IsImageFile(entry.Extension);
    }


    private bool disposedValue;

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Entries = [];
            }

            disposedValue = true;
        }
    }
}
