using System.Text.Json;

namespace DevToys.JsonPowerTool.Helpers;

/// <summary>
/// Generates entity class code from JSON for C#, Java, Python, and Go.
/// Uses template-based generation with support for nested types and arrays.
/// </summary>
internal static class EntityGenerator
{
    /// <summary>
    /// Generates entity class code from the given JSON string in the specified language.
    /// </summary>
    public static string Generate(string json, EntityLanguage language)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            string rootClassName = "RootEntity";

            return language switch
            {
                EntityLanguage.CSharp => GenerateCSharp(root, rootClassName),
                EntityLanguage.Java => GenerateJava(root, rootClassName),
                EntityLanguage.Python => GeneratePython(root, rootClassName),
                EntityLanguage.Go => GenerateGo(root, rootClassName),
                _ => "// Unsupported language"
            };
        }
        catch (JsonException ex)
        {
            return $"// Invalid JSON: {ex.Message}";
        }
    }

    private static string GenerateCSharp(JsonElement element, string className)
    {
        var sb = new System.Text.StringBuilder();
        var nestedClasses = new List<string>();

        sb.AppendLine("using System.Text.Json.Serialization;");
        sb.AppendLine();
        sb.AppendLine($"public class {className}");
        sb.AppendLine("{");

        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToList();
            for (int i = 0; i < properties.Count; i++)
            {
                JsonProperty prop = properties[i];
                string propName = ToPascalCase(prop.Name);
                string typeName = GetCSharpType(prop.Value, propName, nestedClasses);
                sb.AppendLine($"    [JsonPropertyName(\"{prop.Name}\")]");
                sb.AppendLine($"    public {typeName} {propName} {{ get; set; }}");
                if (i < properties.Count - 1)
                    sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        foreach (string nested in nestedClasses)
        {
            sb.AppendLine();
            sb.Append(nested);
        }

        return sb.ToString();
    }

    private static string GenerateJava(JsonElement element, string className)
    {
        var sb = new System.Text.StringBuilder();
        var nestedClasses = new List<string>();

        sb.AppendLine("import com.fasterxml.jackson.annotation.JsonProperty;");
        sb.AppendLine();
        sb.AppendLine($"public class {className} {{");

        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToList();
            // Fields
            foreach (JsonProperty prop in properties)
            {
                string propName = ToCamelCase(prop.Name);
                string typeName = GetJavaType(prop.Value, ToPascalCase(prop.Name), nestedClasses);
                sb.AppendLine($"    @JsonProperty(\"{prop.Name}\")");
                sb.AppendLine($"    private {typeName} {propName};");
            }
            sb.AppendLine();

            // Getters and Setters
            foreach (JsonProperty prop in properties)
            {
                string propName = ToCamelCase(prop.Name);
                string typeName = GetJavaType(prop.Value, ToPascalCase(prop.Name), nestedClasses);
                sb.AppendLine($"    public {typeName} get{ToPascalCase(prop.Name)}() {{ return {propName}; }}");
                sb.AppendLine($"    public void set{ToPascalCase(prop.Name)}({typeName} {propName}) {{ this.{propName} = {propName}; }}");
                sb.AppendLine();
            }
        }

        sb.AppendLine("}");

        foreach (string nested in nestedClasses)
        {
            sb.AppendLine();
            sb.Append(nested);
        }

        return sb.ToString();
    }

    private static string GeneratePython(JsonElement element, string className)
    {
        var sb = new System.Text.StringBuilder();
        var nestedClasses = new List<string>();

        sb.AppendLine("from typing import List, Optional, Any");
        sb.AppendLine("import json");
        sb.AppendLine();
        sb.AppendLine($"class {className}:");

        if (element.ValueKind == JsonValueKind.Object)
        {
            var properties = element.EnumerateObject().ToList();
            if (properties.Count == 0)
            {
                sb.AppendLine("    pass");
            }
            else
            {
                sb.AppendLine("    def __init__(self):");
                foreach (JsonProperty prop in properties)
                {
                    string propName = ToSnakeCase(prop.Name);
                    string typeName = GetPythonType(prop.Value, ToPascalCase(prop.Name), nestedClasses);
                    sb.AppendLine($"        self.{propName}: {typeName} = None");
                }

                sb.AppendLine();
                sb.AppendLine("    @staticmethod");
                sb.AppendLine("    def from_dict(data: dict) -> '{}':".Replace("{}", className));
                sb.AppendLine($"        obj = {className}()");
                foreach (JsonProperty prop in properties)
                {
                    string propName = ToSnakeCase(prop.Name);
                    string dictKey = prop.Name;
                    sb.AppendLine($"        if '{dictKey}' in data:");
                    sb.AppendLine($"            obj.{propName} = data['{dictKey}']");
                }
                sb.AppendLine("        return obj");
            }
        }

        foreach (string nested in nestedClasses)
        {
            sb.AppendLine();
            sb.Append(nested);
        }

        return sb.ToString();
    }

    private static string GenerateGo(JsonElement element, string structName)
    {
        var sb = new System.Text.StringBuilder();
        var nestedStructs = new List<string>();

        sb.AppendLine("package main");
        sb.AppendLine();
        sb.AppendLine("import \"encoding/json\"");
        sb.AppendLine();
        sb.AppendLine($"type {structName} struct {{");

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty prop in element.EnumerateObject())
            {
                string fieldName = ToPascalCase(prop.Name);
                string typeName = GetGoType(prop.Value, fieldName, nestedStructs);
                sb.AppendLine($"    {fieldName} {typeName} `json:\"{prop.Name}\"`");
            }
        }

        sb.AppendLine("}");

        foreach (string nested in nestedStructs)
        {
            sb.AppendLine();
            sb.Append(nested);
        }

        return sb.ToString();
    }

    // --- Type resolution helpers ---

    private static string GetCSharpType(JsonElement element, string suggestedClassName, List<string> nestedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "long" : "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Null => "object?",
            JsonValueKind.Array => GetCSharpArrayType(element, suggestedClassName, nestedClasses),
            JsonValueKind.Object => GetCSharpObjectType(element, suggestedClassName, nestedClasses),
            _ => "object"
        };
    }

    private static string GetCSharpArrayType(JsonElement array, string className, List<string> nestedClasses)
    {
        if (array.GetArrayLength() == 0)
            return "List<object>";

        JsonElement first = array[0];
        if (first.ValueKind == JsonValueKind.Object)
        {
            string itemType = className + "Item";
            nestedClasses.Add(GenerateCSharp(first, itemType));
            return $"List<{itemType}>";
        }
        if (first.ValueKind == JsonValueKind.Array)
            return "List<List<object>>";

        string baseType = first.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => first.TryGetInt64(out _) ? "long" : "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            _ => "object"
        };
        return $"List<{baseType}>";
    }

    private static string GetCSharpObjectType(JsonElement obj, string className, List<string> nestedClasses)
    {
        nestedClasses.Add(GenerateCSharp(obj, className));
        return className;
    }

    private static string GetJavaType(JsonElement element, string suggestedClassName, List<string> nestedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "Long" : "Double",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            JsonValueKind.Null => "Object",
            JsonValueKind.Array => GetJavaArrayType(element, suggestedClassName, nestedClasses),
            JsonValueKind.Object => GetJavaObjectType(element, suggestedClassName, nestedClasses),
            _ => "Object"
        };
    }

    private static string GetJavaArrayType(JsonElement array, string className, List<string> nestedClasses)
    {
        if (array.GetArrayLength() == 0)
            return "List<Object>";

        JsonElement first = array[0];
        if (first.ValueKind == JsonValueKind.Object)
        {
            string itemType = className + "Item";
            nestedClasses.Add(GenerateJava(first, itemType));
            return $"List<{itemType}>";
        }
        if (first.ValueKind == JsonValueKind.Array)
            return "List<List<Object>>";

        string baseType = first.ValueKind switch
        {
            JsonValueKind.String => "String",
            JsonValueKind.Number => first.TryGetInt64(out _) ? "Long" : "Double",
            JsonValueKind.True or JsonValueKind.False => "Boolean",
            _ => "Object"
        };
        return $"List<{baseType}>";
    }

    private static string GetJavaObjectType(JsonElement obj, string className, List<string> nestedClasses)
    {
        nestedClasses.Add(GenerateJava(obj, className));
        return className;
    }

    private static string GetPythonType(JsonElement element, string suggestedClassName, List<string> nestedClasses)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "str",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "int" : "float",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Null => "Any",
            JsonValueKind.Array => "list",
            JsonValueKind.Object => GetPythonObjectType(element, suggestedClassName, nestedClasses),
            _ => "Any"
        };
    }

    private static string GetPythonObjectType(JsonElement obj, string className, List<string> nestedClasses)
    {
        nestedClasses.Add(GeneratePython(obj, className));
        return className;
    }

    private static string GetGoType(JsonElement element, string suggestedStructName, List<string> nestedStructs)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "int64" : "float64",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Null => "interface{}",
            JsonValueKind.Array => GetGoSliceType(element, suggestedStructName, nestedStructs),
            JsonValueKind.Object => GetGoStructType(element, suggestedStructName, nestedStructs),
            _ => "interface{}"
        };
    }

    private static string GetGoSliceType(JsonElement array, string structName, List<string> nestedStructs)
    {
        if (array.GetArrayLength() == 0)
            return "[]interface{}";

        JsonElement first = array[0];
        if (first.ValueKind == JsonValueKind.Object)
        {
            string itemStruct = structName + "Item";
            nestedStructs.Add(GenerateGo(first, itemStruct));
            return $"[]{itemStruct}";
        }
        if (first.ValueKind == JsonValueKind.Array)
            return "[][]interface{}";

        string baseType = first.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => first.TryGetInt64(out _) ? "int64" : "float64",
            JsonValueKind.True or JsonValueKind.False => "bool",
            _ => "interface{}"
        };
        return $"[]{baseType}";
    }

    private static string GetGoStructType(JsonElement obj, string structName, List<string> nestedStructs)
    {
        nestedStructs.Add(GenerateGo(obj, structName));
        return structName;
    }

    // --- String helpers ---

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var parts = input.Split('_', '-', ' ');
        return string.Concat(parts.Select(p =>
            string.IsNullOrEmpty(p) ? p : char.ToUpper(p[0]) + p[1..].ToLower()));
    }

    private static string ToCamelCase(string input)
    {
        string pascal = ToPascalCase(input);
        return string.IsNullOrEmpty(pascal) ? pascal : char.ToLower(pascal[0]) + pascal[1..];
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new System.Text.StringBuilder();
        foreach (char c in input)
        {
            if (char.IsUpper(c))
            {
                if (sb.Length > 0) sb.Append('_');
                sb.Append(char.ToLower(c));
            }
            else if (c == '-' || c == ' ')
            {
                sb.Append('_');
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
