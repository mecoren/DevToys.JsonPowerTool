using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Path;
using DevLab.JmesPath;

namespace DevToys.JsonPowerTool.Helpers;

/// <summary>
/// Handles JSONPath and JMESPath queries against a JSON document.
/// </summary>
internal static class JsonQueryEngine
{
    /// <summary>
    /// Executes a JSONPath query using JsonPath.Net and returns the matching JSON fragment.
    /// </summary>
    public static string QueryJsonPath(string json, string expression)
    {
        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
                return string.Empty;

            var path = JsonPath.Parse(expression);
            PathResult result = path.Evaluate(node);

            if (result.Matches == null || !result.Matches.Any())
                return string.Empty;

            var matches = result.Matches.ToList();
            if (matches.Count == 1)
                return matches[0].Value?.ToJsonString() ?? string.Empty;

            var array = new JsonArray();
            foreach (var match in matches)
            {
                array.Add(JsonNode.Parse(match.Value?.ToJsonString() ?? "null"));
            }
            return array.ToJsonString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Executes a JMESPath query using JmesPath.Net and returns the matching JSON fragment.
    /// </summary>
    public static string QueryJmesPath(string json, string expression)
    {
        try
        {
            var jmes = new JmesPath();
            string result = jmes.Transform(json, expression);
            return result;
        }
        catch
        {
            return string.Empty;
        }
    }
}
