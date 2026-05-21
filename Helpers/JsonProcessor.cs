using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace DevToys.JsonPowerTool.Helpers;

/// <summary>
/// Core JSON processing logic: beautify, minify, sort.
/// Uses System.Text.Json for parsing and formatting.
/// </summary>
internal static class JsonProcessor
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly JsonWriterOptions BeautifyOptions = new()
    {
        Indented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonWriterOptions MinifyOptions = new()
    {
        Indented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Beautifies (pretty-prints) the given JSON string.
    /// </summary>
    public static string Beautify(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, BeautifyOptions);
        doc.WriteTo(writer);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Minifies (compacts) the given JSON string.
    /// </summary>
    public static string Minify(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, MinifyOptions);
        doc.WriteTo(writer);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Recursively sorts all keys in a JSON object alphabetically.
    /// Array elements that are objects are also sorted internally,
    /// but non-object array elements maintain their original order.
    /// </summary>
    public static string SortByKey(string json, SortDirection direction)
    {
        using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
        JsonElement root = doc.RootElement;
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream, BeautifyOptions);
        WriteElement(writer, root, direction);
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Validates JSON and returns error information if invalid.
    /// </summary>
    public static JsonValidationError? Validate(string json)
    {
        try
        {
            JsonDocument.Parse(json, DocumentOptions);
            return null;
        }
        catch (JsonException ex)
        {
            return new JsonValidationError(
                ex.Message,
                ex.LineNumber.HasValue ? (int)ex.LineNumber + 1 : -1,
                ex.BytePositionInLine.HasValue ? (int)ex.BytePositionInLine + 1 : -1
            );
        }
    }

    /// <summary>
    /// Checks whether the JSON is an array of homogeneous objects (suitable for table view).
    /// Returns the column headers (union of all keys) if applicable, or null otherwise.
    /// </summary>
    public static string[]? GetTableColumns(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() == 0)
                return null;

            var allKeys = new HashSet<string>();
            foreach (JsonElement item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return null;
                foreach (JsonProperty prop in item.EnumerateObject())
                    allKeys.Add(prop.Name);
            }
            return allKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts table data from a JSON array of objects.
    /// Each row is a dictionary mapping column name to string value.
    /// </summary>
    public static List<Dictionary<string, string>>? GetTableRows(string json, string[] columns)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
            JsonElement root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return null;

            var rows = new List<Dictionary<string, string>>();
            foreach (JsonElement item in root.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    return null;
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (string col in columns)
                {
                    if (item.TryGetProperty(col, out JsonElement val))
                        row[col] = ValueToString(val);
                    else
                        row[col] = string.Empty;
                }
                rows.Add(row);
            }
            return rows;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Builds a tree structure from JSON for the tree view.
    /// </summary>
    public static JsonTreeNode BuildTree(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json, DocumentOptions);
        return BuildNode("$", doc.RootElement, "$");
    }

    /// <summary>
    /// Converts XML string to JSON string using Newtonsoft.Json.
    /// Returns the pretty-printed JSON, or throws on invalid XML.
    /// </summary>
    public static string XmlToJson(string xml)
    {
        XDocument xdoc = XDocument.Parse(xml);
        string json = Newtonsoft.Json.JsonConvert.SerializeXNode(xdoc, Newtonsoft.Json.Formatting.Indented);
        return json;
    }

    /// <summary>
    /// Validates XML and returns error information if invalid.
    /// </summary>
    public static JsonValidationError? ValidateXml(string xml)
    {
        try
        {
            XDocument.Parse(xml);
            return null;
        }
        catch (XmlException ex)
        {
            return new JsonValidationError(
                ex.Message,
                ex.LineNumber > 0 ? ex.LineNumber : -1,
                ex.LinePosition > 0 ? ex.LinePosition : -1
            );
        }
    }

    /// <summary>
    /// Converts a JsonElement value to a display string.
    /// </summary>
    public static string ValueToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? "",
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => "null",
            JsonValueKind.Object => $"{{…}} {element.EnumerateObject().Count()} keys",
            JsonValueKind.Array => $"[…] {element.GetArrayLength()} items",
            _ => element.GetRawText()
        };
    }

    // --- Private helpers ---

    private static JsonTreeNode BuildNode(string name, JsonElement element, string path)
    {
        var node = new JsonTreeNode
        {
            Name = name,
            Path = path,
            Type = element.ValueKind.ToString(),
            RawValue = element.ValueKind switch
            {
                JsonValueKind.Object => $"{{…}} {element.EnumerateObject().Count()} keys",
                JsonValueKind.Array => $"[…] {element.GetArrayLength()} items",
                _ => ValueToString(element)
            }
        };

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                string childPath = $"{path}.{EscapeJsonPathKey(prop.Name)}";
                node.Children.Add(BuildNode(prop.Name, prop.Value, childPath));
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement item in element.EnumerateArray())
            {
                string childPath = $"{path}[{i}]";
                node.Children.Add(BuildNode($"[{i}]", item, childPath));
                i++;
            }
        }

        return node;
    }

    private static string EscapeJsonPathKey(string key)
    {
        if (key.Any(c => char.IsWhiteSpace(c) || c is '.' or '[' or ']' or '\'' or '"'))
            return $"['{key.Replace("'", "\\'")}']";
        return key;
    }

    private static void WriteElement(Utf8JsonWriter writer, JsonElement element, SortDirection sortDirection)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                WriteSortedObject(writer, element, sortDirection);
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (JsonElement item in element.EnumerateArray())
                    WriteElement(writer, item, sortDirection);
                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                if (element.TryGetInt64(out long longVal))
                    writer.WriteNumberValue(longVal);
                else if (element.TryGetDouble(out double doubleVal))
                    writer.WriteNumberValue(doubleVal);
                else if (element.TryGetDecimal(out decimal decVal))
                    writer.WriteNumberValue(decVal);
                else
                    writer.WriteRawValue(element.GetRawText());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
                writer.WriteNullValue();
                break;
            default:
                writer.WriteRawValue(element.GetRawText());
                break;
        }
    }

    private static void WriteSortedObject(Utf8JsonWriter writer, JsonElement obj, SortDirection direction)
    {
        var properties = obj.EnumerateObject().ToList();
        if (direction == SortDirection.Ascending)
            properties.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
        else
            properties.Sort((a, b) => string.Compare(b.Name, a.Name, StringComparison.Ordinal));

        writer.WriteStartObject();
        foreach (JsonProperty prop in properties)
        {
            writer.WritePropertyName(prop.Name);
            WriteElement(writer, prop.Value, direction);
        }
        writer.WriteEndObject();
    }
}

/// <summary>
/// Represents a JSON validation error with line and column information.
/// </summary>
internal sealed record JsonValidationError(string Message, int Line, int Column);

/// <summary>
/// Represents a node in the JSON tree structure.
/// </summary>
internal sealed class JsonTreeNode
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string RawValue { get; set; } = "";
    public List<JsonTreeNode> Children { get; } = new();
    public string Path { get; set; } = "";
}
