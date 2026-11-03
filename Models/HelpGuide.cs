namespace _2Korriku.Models;

public sealed class HelpModule
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public List<string> Steps { get; set; } = [];
    public List<string> Notes { get; set; } = [];
    public Dictionary<string, string> Pages { get; set; } = [];
    public Dictionary<string, HelpDetail> Details { get; set; } = [];
}
public sealed class HelpDetail
{
    public string Summary { get; set; } = "";
    public List<string> Steps { get; set; } = [];
    public List<string> Notes { get; set; } = [];
}
public sealed record HelpGuide(string Id, string Module, string Title, string Summary,
    IReadOnlyList<string> Steps, IReadOnlyList<string> Notes);
public sealed record HelpIndex(IReadOnlyList<HelpGuide> Guides, string? Query, int Total);
