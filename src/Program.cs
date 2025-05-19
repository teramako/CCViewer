using System.Globalization;

if (args.Length < 1)
    Environment.Exit(1);

using var viewer = GetViewer(args);

Console.Clear();
Task keyTask = WaitInput();
viewer.Show(0);
keyTask.Wait();
Environment.Exit(0);

IImageViewer GetViewer(string[] files)
{
    if (files.Length == 0)
        throw new FileNotFoundException();

    if (files.Length == 1)
    {
        var file = files[0];
        if (!Path.Exists(file))
            throw new FileNotFoundException("No such file or directory", file);

        var fileAttr = File.GetAttributes(file);
        if (fileAttr.HasFlag(FileAttributes.Directory))
        {
            return new FilesViewer(new DirectoryInfo(file));
        }
        var ext = Path.GetExtension(file);
        switch (ext.ToUpperInvariant())
        {
            case ".ZIP":
                return new ZipViewer(new FileInfo(file));
        }
    }

    return new FilesViewer(files.Select(f => new FileInfo(f)));
}

async Task WaitInput()
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

void PrintKeyBinding(int cursorLeft = 0, int cursorTop = 5)
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
