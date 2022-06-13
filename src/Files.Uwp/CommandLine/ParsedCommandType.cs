namespace Files.Uwp.CommandLine
{
    internal enum ParsedCommandType
    {
        Unknown,
        OpenDirectory,
        OpenPath,
        ExplorerShellCommand,
        OutputPath,
        SelectItem,
        TagFiles
    }
}