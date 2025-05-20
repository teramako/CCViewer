using CommandLine;

internal class Options
{
    [Option('p', "page", Required = false, Default = 1,
            HelpText = "Page number to display (starting with 1)")]
    public int Page { get; set; }

    [Value(0, MetaName = "files",
           HelpText = "a zip file or a directory or image files")]
    public IEnumerable<string> Files { get; set; } = [];
}
