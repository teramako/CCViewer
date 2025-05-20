using CommandLine;

internal class Options
{
    [Value(0, MetaName = "files",
           HelpText = "a zip file or a directory or image files")]
    public IEnumerable<string> Files { get; set; } = [];
}
