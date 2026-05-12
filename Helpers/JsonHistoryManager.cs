using System.Text.Json;

namespace DevToys.JsonPowerTool.Helpers;

/// <summary>
/// Manages a history of recent JSON inputs, stored in the local cache directory.
/// Persists the last 20 entries.
/// </summary>
internal sealed class JsonHistoryManager
{
    private const int MaxEntries = 20;
    private readonly string _historyFilePath;
    private readonly List<string> _entries = new();

    public IReadOnlyList<string> Entries => _entries;

    public JsonHistoryManager(string cacheDirectory)
    {
        _historyFilePath = Path.Combine(cacheDirectory, "json_powertool_history.json");
        Load();
    }

    /// <summary>
    /// Adds a new JSON input to the history. If it already exists, moves it to the top.
    /// Trims to MaxEntries.
    /// </summary>
    public void Add(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        // Remove duplicate if exists
        _entries.Remove(json);

        // Insert at the beginning
        _entries.Insert(0, json);

        // Trim to max
        while (_entries.Count > MaxEntries)
            _entries.RemoveAt(_entries.Count - 1);

        Save();
    }

    /// <summary>
    /// Gets a short preview of a JSON entry for display in the dropdown.
    /// </summary>
    public static string GetPreview(string json, int maxLength = 80)
    {
        if (string.IsNullOrEmpty(json))
            return string.Empty;

        string preview = json.Replace('\n', ' ').Replace('\r', ' ').Trim();
        if (preview.Length > maxLength)
            preview = preview[..maxLength] + "…";
        return preview;
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_historyFilePath))
                return;

            string json = File.ReadAllText(_historyFilePath);
            var loaded = JsonSerializer.Deserialize<List<string>>(json);
            if (loaded is not null)
                _entries.AddRange(loaded);
        }
        catch
        {
            // Ignore errors; start with empty history
        }
    }

    private void Save()
    {
        try
        {
            string? dir = Path.GetDirectoryName(_historyFilePath);
            if (dir is not null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = false });
            File.WriteAllText(_historyFilePath, json);
        }
        catch
        {
            // Ignore errors; history is not critical
        }
    }
}
