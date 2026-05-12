namespace DevToys.JsonPowerTool;

/// <summary>
/// The available processing modes for the JSON PowerTool.
/// </summary>
internal enum JsonProcessingMode
{
    Beautify,
    Minify,
    TreeView,
    TableView,
    Query,
    EntityConvert,
    Sort
}

/// <summary>
/// The supported query languages.
/// </summary>
internal enum QueryLanguage
{
    JsonPath,
    JmesPath
}

/// <summary>
/// The supported entity generation languages.
/// </summary>
internal enum EntityLanguage
{
    CSharp,
    Java,
    Python,
    Go
}

/// <summary>
/// Sort direction for JSON key sorting.
/// </summary>
internal enum SortDirection
{
    Ascending,
    Descending
}
