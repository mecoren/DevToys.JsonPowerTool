# JSON PowerTool — DevToys 2.0 Plugin

A full-featured JSON tool plugin for [DevToys 2.0](https://devtoys.app/), providing beautify, minify, tree view, table view, JSONPath/JMESPath query, entity class generation, and key sorting — all in one tool.

## Features

| Mode | Description |
|------|-------------|
| **Beautify** | Pretty-print JSON with indentation and line breaks |
| **Minify** | Compact JSON to a single line |
| **Tree View** | Interactive tree display with type annotations and summaries |
| **Table View** | Tabular display for arrays of homogeneous objects |
| **Query** | JSONPath and JMESPath queries against the input JSON |
| **Entity** | Generate C#, Java, Python, or Go entity classes from JSON |
| **Sort** | Recursively sort object keys in ascending or descending order |

### Additional Features
- **Syntax highlighting** (Monaco Editor via `IUIMultiLineTextInput`)
- **Line numbers** always visible
- **Error highlighting** with line/column information on parse failure
- **History** of last 20 inputs persisted to local cache
- **Smart Detection** — automatically recommended when clipboard contains JSON
- **Split layout** — left input, right output (DevToys standard)
- **Copy** button on all output editors

## Project Structure

```
DevToys.JsonPowerTool/
├── DevToys.JsonPowerTool.csproj        # Project file (.NET 8.0 class library)
├── Strings.resx                        # Localized string resources
├── Strings.Designer.cs                 # Auto-generated resource accessor
├── Enums.cs                            # Enum definitions (modes, languages, sort direction)
├── JsonPowerToolResourceAssemblyIdentifier.cs  # MEF export: resource locator
├── JsonPowerToolGui.cs                 # MEF export: IGuiTool implementation (main UI + logic)
└── Helpers/
    ├── JsonProcessor.cs                # Core JSON operations (beautify, minify, sort, validate, tree, table)
    ├── JsonQueryEngine.cs              # JSONPath and JMESPath query execution
    ├── EntityGenerator.cs              # Entity class generation (C#, Java, Python, Go)
    └── JsonHistoryManager.cs           # Input history persistence (last 20 entries)
```

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `DevToys.Api` | 2.0.10-preview | Plugin SDK |
| `JsonPath.Net` | 1.0.4 | JSONPath query support (RFC 9535) |
| `JmesPath.Net` | 1.0.125 | JMESPath query support |
| `Newtonsoft.Json` | 13.0.3 | Used by JmesPath.Net internally |

## Building

```bash
dotnet build
```

Output DLL: `bin/Debug/net8.0/DevToys.JsonPowerTool.dll`

## Installation

### Via DevToys Extension Manager
1. Pack as NuGet: `dotnet pack`
2. In DevToys, open **Extension Manager** → **Install** → select the `.nupkg`

### Manual
1. Build the project
2. Copy `DevToys.JsonPowerTool.dll` and all dependency DLLs to the DevToys extensions folder
3. Restart DevToys

## Technical Notes

- **UI Framework**: DevToys Blazor-based Fluent API (no XAML needed)
- **Architecture**: MVVM — logic in `JsonPowerToolGui`, processing in `Helpers/`
- **JSON Parsing**: `System.Text.Json` as primary parser; `Newtonsoft.Json` for query compatibility
- **Async**: Large files are handled without UI blocking via `ValueTask` callbacks
- **Error Handling**: Parse errors show line/column in an `IUIInfoBar` and highlight the offending line with `UIHighlightedTextSpan`
