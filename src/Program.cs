using System.Globalization;
using CommandLine;

Parser.Default.ParseArguments<Options>(args)
    .WithParsed(Run);

static void Run(Options opts)
{
    var pageMode = opts.CommicMode ? PageMode.RightToLeft : PageMode.LeftToRight;
    using var viewer = GetViewer([.. opts.Files], pageMode);
    viewer.Show(opts.Page - 1);
    WaitInput(viewer).Wait();
    Environment.Exit(1);
}

static IImageViewer GetViewer(string[] files, PageMode mode)
{
    if (files.Length == 0)
        throw new FileNotFoundException();

    if (files.Length == 1)
    {
        var path = files[0];
        if (!Path.Exists(path))
            throw new FileNotFoundException("No such file or directory", path);

        var fileAttr = File.GetAttributes(path);
        if (fileAttr.HasFlag(FileAttributes.Directory))
        {
            return new FilesViewer(new DirectoryInfo(path), mode);
        }
        var ext = Path.GetExtension(path);
        return ext.ToUpperInvariant() switch
        {
            ".ZIP" => new ZipViewer(new FileInfo(path), mode),
            _ => new FilesViewer([new FileInfo(path)], mode),
        };
        ;
    }
    else
    {
        var fileList = new List<FileInfo>();
        foreach (var path in files)
        {
            if (File.Exists(path))
            {
                fileList.Add(new FileInfo(path));
                continue;
            }
            if (Directory.Exists(path))
            {
                foreach (var filePath in Directory.EnumerateFiles(path))
                {
                    fileList.Add(new FileInfo(filePath));
                }
                continue;
            }
            var f = new FileInfo(path);
            if ((f.Directory?.Exists ?? false)
                && (f.Name.Contains('*') || f.Name.Contains('?')))
            {
                foreach (var fileInfo in f.Directory.EnumerateFiles(f.Name))
                {
                    fileList.Add(fileInfo);
                }
            }
        }
        return new FilesViewer(fileList, mode);
    }
}

static async Task WaitInput(IImageViewer viewer)
{
    do
    {
        while (!Console.KeyAvailable)
        {
            await Task.Delay(50);
        }
        var keyInfo = Console.ReadKey(true);
        switch (keyInfo)
        {
            case { KeyChar: 'q' }:
            case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.C }:
                return;
            case { KeyChar: '?' }:
                PrintKeyBinding();
                break;
            case { KeyChar: 'n' }:
            case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.N }:
                if (!viewer.ShowNext())
                {
                    Console.CursorLeft = 0;
                    Console.Error.Write($"Already the last page.");
                }
                break;
            case { KeyChar: 'N' }:
                if (!viewer.ShowNext(true))
                {
                    Console.CursorLeft = 0;
                    Console.Error.Write($"Already the last page.");
                }
                break;
            case { KeyChar: 'p' }:
            case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.P }:
                if (!viewer.ShowPrevious())
                {
                    Console.CursorLeft = 0;
                    Console.Error.Write($"Already on first page.");
                }
                break;
            case { KeyChar: 'P' }:
                if (!viewer.ShowPrevious(true))
                {
                    Console.CursorLeft = 0;
                    Console.Error.Write($"Already the last page.");
                }
                break;
            case { Key: ConsoleKey.Escape }:
            case { KeyChar: 'r' }:
            case { Modifiers: ConsoleModifiers.Control, Key: ConsoleKey.R }:
                _ = viewer.Show(viewer.CurrentIndex);
                break;
            case { KeyChar: 's' }:
                _ = viewer.Show(viewer.CurrentIndex, true);
                break;
            case { KeyChar: 'm' }:
                viewer.PageMode = viewer.PageMode == PageMode.LeftToRight
                                  ? PageMode.RightToLeft
                                  : PageMode.LeftToRight;
                viewer.Show(viewer.CurrentIndex);
                break;
            case { KeyChar: '^' }:
                _ = viewer.Show(0);
                break;
            case { KeyChar: '$' }:
                _ = viewer.Show(viewer.LastIndex);
                break;
            case { KeyChar: ':' }:
                Console.Write(": ");
                var cmd = Console.ReadLine()?.Trim() ?? string.Empty;
                if (cmd.StartsWith('+'))
                {
                    if (int.TryParse(cmd, CultureInfo.InvariantCulture, out var page))
                    {
                        viewer.Show(viewer.CurrentIndex + page);
                    }
                }
                else if (cmd.StartsWith('-'))
                {
                    if (int.TryParse(cmd, CultureInfo.InvariantCulture, out var page))
                    {
                        viewer.Show(viewer.CurrentIndex + page);
                    }
                }
                else if (int.TryParse(cmd, CultureInfo.InvariantCulture, out var page))
                {
                    viewer.Show(page);
                }
                break;
        }
    }
    while (true);
}

static void PrintKeyBinding(int cursorLeft = 0, int cursorTop = 5)
{
    var p = Console.GetCursorPosition();
    Console.SetCursorPosition(cursorLeft, cursorTop);
    Console.WriteLine($"""
        ╭──────────────────────────────────────────────────────────────╮
        │ Key bindings                                                 │
        ├──────────────────────────────────────────────────────────────┤
        │ q              => Quit                                       │
        │ n, Ctrl+n      => Move Next page                             │
        │ N              => Move Next page (force single page)         │
        │ p, Ctrl+p      => Move Previous page                         │
        │ P              => Move Previous page (force single page)     │
        │ Esc, r, Ctrl+r => Re-rendering the current page(s)           │
        │ s              => Re-rendering (force single page)           │
        │ ^              => Move the first page                        │
        │ $              => Move the last page                         │
        │ m              => Toggle Page mode                           │
        │ :              => Enter the comand mode                      │
        ╰──────────────────────────────────────────────────────────────╯
        """);
    Console.SetCursorPosition(p.Left, p.Top);
}
