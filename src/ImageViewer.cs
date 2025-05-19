using SixLabors.ImageSharp;
using SixPix;
using SixPix.Encoder;

internal enum PageState
{
    None = 0,
    Single = 1,
    Double = 2,
}

internal enum PageMode
{
    /// <summary>
    /// Normal mode;
    /// </summary>
    LeftToRight,
    /// <summary>
    /// Japanese Comic mode
    /// </summary>
    RightToLeft,
}

internal interface IImageViewer : IDisposable
{
    int CurrentIndex { get; }
    int LastIndex { get; }
    PageMode PageMode { get; set; }

    bool Show(int index, bool forceSingle = false);
    bool ShowNext(bool forceSingle = false);
    bool ShowPrevious(bool forceSingle = false);
}

internal abstract class ImageViewer<TEntry> : IImageViewer where TEntry : IEntry
{
    public static bool IsImageFile(string extension)
    {
        return extension.ToUpperInvariant() is
            ".GIF" or ".PNG" or ".JPG" or ".JPEG" or ".WEBP" or ".BMP";
    }
    /// <summary>
    /// Cursor pixel size
    /// </summary>
    protected static readonly Size CellSize = Sixel.GetCellSize();

    /// <summary>
    /// Get the adjusted size of the image to fit in the window and cell width unit
    /// </summary>
    /// <param name="imgSize">Image size</param>
    /// <param name="winSize">Windows size</param>
    /// <param name="cellSize">Cell(Cursor) size</param>
    /// <returns></returns>
    protected static Size GetAdjustSize(Size imgSize, Size winSize, Size cellSize)
    {
        var width = imgSize.Width;
        var maxWidth = Math.Min(imgSize.Width, winSize.Width);
        var maxHeight = Math.Min(imgSize.Height, winSize.Height - (CellSize.Height * 3));
        double ratio = 1.0;
        if (maxWidth < imgSize.Width || maxHeight < imgSize.Height)
        {
            ratio = Math.Min(1.0 * maxWidth / imgSize.Width, 1.0 * maxHeight / imgSize.Height);
            // adjust width to fit in the cell width
            width = (int)(imgSize.Width * ratio);
            width -= width % cellSize.Width;
            // recalculate ratio
            ratio = 1.0 * width / imgSize.Width;
        }
        if (imgSize.Width > cellSize.Width)
        {
            width -= width % cellSize.Width;
            ratio = 1.0 * width / imgSize.Width;
        }
        var height = (int)(imgSize.Height * ratio);
        return new(width, height);
    }

    protected static async Task RenderImage(SixelEncoder encoder,
                                            int cursorLeft = 0,
                                            int cursorTop = 0,
                                            string header = "")
    {
        Console.SetCursorPosition(cursorLeft, cursorTop);
        await Console.Out.WriteAsync(header);
        Console.SetCursorPosition(cursorLeft, cursorTop + 1);
        await Console.Out.WriteLineAsync(encoder.Encode());
    }

    /// <summary>
    /// Image file entires
    /// </summary>
    protected TEntry[] Entries { get; set; } = [];

    public int CurrentIndex { get; protected set; }
    public int LastIndex => Entries.Length - 1;

    public PageMode PageMode { get; set; } = PageMode.RightToLeft;
    protected PageState PageState { get; set; } = PageState.None;

    /// <summary>
    /// Display next page(s).
    /// </summary>
    /// <param name="forceSingle">Force the display of only single page</param>.
    /// <returns><c>true</c> when showing next pages is Success, otherwise <c>false</c></returns>
    public bool ShowNext(bool forceSingle = false)
    {
        int index = CurrentIndex + (int)PageState;
        bool hasNext = index < Entries.Length;

        if (!hasNext)
            return false;

        return Show(index, forceSingle);
    }

    /// <summary>
    /// Display previous page(s).
    /// </summary>
    /// <param name="forceSingle">Force the display of only single page</param>.
    /// <returns><c>true</c> when showing previous pages is Success, otherwise <c>false</c></returns>
    public bool ShowPrevious(bool forceSingle = false)
    {
        int index = CurrentIndex - 1;
        bool hasPrev1 = index >= 0;

        if (!hasPrev1)
            return false;

        return Show(index, forceSingle);
    }

    /// <summary>
    /// Show the page in the <paramref name="index"/>, plus the next or previous page if available
    /// </summary>
    /// <param name="index"></param>
    /// <param name="forceSingle">Force the display of only single page</param>.
    /// <returns><c>true</c> when showing the pages is Success, otherwise <c>false</c></returns>
    public bool Show(int index, bool forceSingle = false)
    {
        if (index < 0 || index > Entries.Length - 1)
            return false;

        bool forward = index >= CurrentIndex;

        var entry1 = Entries[index];
        CurrentIndex = index;
        Size winSize = Sixel.GetWindowPixelSize();
        using Stream st1 = entry1.Open();
        using SixelEncoder enc1 = Sixel.CreateEncoder(st1);
        Size size1 = GetAdjustSize(enc1.CanvasSize, winSize, CellSize);
        enc1.Resize(size1);
        string subject1 = $"{index + 1,3:d}/{Entries.Length,3:d}";

        (int cLeft, int cTop) = (0, 0);

        // clear screen
        Console.SetCursorPosition(cLeft, cTop);
        Console.Write($"{(char)0x1b}[0J");

        PageState = PageState.Single;
        if (!forceSingle && size1.Height > size1.Width)
        {
            if (forward && index + 1 < Entries.Length)
            {
                var entry2 = Entries[index + 1];
                using Stream st2 = entry2.Open();
                using SixelEncoder enc2 = Sixel.CreateEncoder(st2);
                Size size2 = GetAdjustSize(enc2.CanvasSize, winSize, CellSize);
                enc2.Resize(size2);
                string subject2 = $"{index + 2,3:d}/{Entries.Length,3:d}";
                if (size2.Height > size2.Width && size1.Width + size2.Width < winSize.Width)
                {
                    PageState = PageState.Double;
                    switch (PageMode)
                    {
                        case PageMode.RightToLeft:
                            cLeft = (int)Math.Ceiling((double)size2.Width / CellSize.Width);
                            Task.WaitAll([
                                RenderImage(enc1, cLeft, cTop, subject1),
                                RenderImage(enc2, 0, cTop, subject2),
                            ]);
                            return true;
                        case PageMode.LeftToRight:
                        default:
                            cLeft = (int)Math.Ceiling((double)size1.Width / CellSize.Width);
                            Task.WaitAll([
                                RenderImage(enc1, 0, cTop, subject1),
                                RenderImage(enc2, cLeft, cTop, subject2),
                            ]);
                            return true;
                    }
                }
            }
            else if (!forward && index - 1 >= 0)
            {
                var entry2 = Entries[index - 1];
                using Stream st2 = entry2.Open();
                using SixelEncoder enc2 = Sixel.CreateEncoder(st2);
                Size size2 = GetAdjustSize(enc2.CanvasSize, winSize, CellSize);
                enc2.Resize(size2);
                string subject2 = $"{index,3:d}/{Entries.Length,3:d}";
                if (size2.Height > size2.Width && size1.Width + size2.Width < winSize.Width)
                {
                    CurrentIndex = index - 1;
                    PageState = PageState.Double;
                    switch (PageMode)
                    {
                        case PageMode.RightToLeft:
                            cLeft = (int)Math.Ceiling((double)size1.Width / CellSize.Width);
                            Task.WaitAll([
                                RenderImage(enc2, cLeft, cTop, subject2),
                                RenderImage(enc1, 0, cTop, subject1),
                            ]);
                            return true;
                        case PageMode.LeftToRight:
                        default:
                            cLeft = (int)Math.Ceiling((double)size2.Width / CellSize.Width);
                            Task.WaitAll([
                                RenderImage(enc2, 0, cTop, subject2),
                                RenderImage(enc1, cLeft, cTop, subject1),
                            ]);
                            return true;
                    }
                }
            }
        }
        RenderImage(enc1, cLeft, cTop, subject1).Wait();
        return true;
    }

    protected abstract void Dispose(bool disposing);

    ~ImageViewer()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
