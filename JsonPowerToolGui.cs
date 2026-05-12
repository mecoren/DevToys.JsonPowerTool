using System.ComponentModel.Composition;
using System.Text;
using DevToys.Api;
using static DevToys.Api.GUI;
using DevToys.JsonPowerTool.Helpers;

namespace DevToys.JsonPowerTool;

[Export(typeof(IGuiTool))]
[Name("JsonPowerTool")]
[ToolDisplayInformation(
    IconFontName = "FluentSystemIcons",
    IconGlyph = '\uE94D',
    GroupName = PredefinedCommonToolGroupNames.Formatters,
    ResourceManagerAssemblyIdentifier = nameof(JsonPowerToolResourceAssemblyIdentifier),
    ResourceManagerBaseName = "DevToys.JsonPowerTool.Strings",
    ShortDisplayTitleResourceName = nameof(Strings.ShortDisplayTitle),
    LongDisplayTitleResourceName = nameof(Strings.LongDisplayTitle),
    DescriptionResourceName = nameof(Strings.Description),
    AccessibleNameResourceName = nameof(Strings.AccessibleName))]
[AcceptedDataTypeName(PredefinedCommonDataTypeNames.Json)]
internal sealed class JsonPowerToolGui : IGuiTool
{
    // --- UI component references ---
    private IUIMultiLineTextInput _inputEditor = default!;
    private IUIMultiLineTextInput _outputEditor = default!;
    private IUISelectDropDownList _queryLanguageSelector = default!;
    private IUISingleLineTextInput _queryExpressionInput = default!;
    private IUISelectDropDownList _entityLanguageSelector = default!;
    private IUIDataGrid _tableViewGrid = default!;
    private IUIMultiLineTextInput _treeViewEditor = default!;
    private IUIMultiLineTextInput _queryResultEditor = default!;
    private IUIMultiLineTextInput _entityResultEditor = default!;
    private IUIMultiLineTextInput _sortResultEditor = default!;
    private IUIElement _beautifyMinifyPanel = default!;
    private IUIElement _treeViewPanel = default!;
    private IUIElement _tableViewPanel = default!;
    private IUIElement _queryPanel = default!;
    private IUIElement _entityPanel = default!;
    private IUIElement _sortPanel = default!;
    private IUILabel _treePathLabel = default!;
    private IUILabel _treeValueLabel = default!;
    private IUIStack _modeButtonBar = default!;

    // --- State ---
    private JsonProcessingMode _currentMode = JsonProcessingMode.Beautify;
    private SortDirection _sortDirection = SortDirection.Ascending;
    private bool _uiBuilt;
    private IUIElement _mainLayout = default!;

    // Debounce timer for input changes
    private CancellationTokenSource? _debounceCts;

    private static readonly IUIDropDownListItem[] QueryLanguageItems = new[]
    {
        Item(Strings.QueryLanguageJsonPath),
        Item(Strings.QueryLanguageJMESPath)
    };

    private static readonly IUIDropDownListItem[] EntityLanguageItems = new[]
    {
        Item("C#"),
        Item("Java"),
        Item("Python"),
        Item("Go")
    };

    public UIToolView View
    {
        get
        {
            if (!_uiBuilt)
            {
                BuildUI();
                _uiBuilt = true;
            }
            return new UIToolView(isScrollable: false, _mainLayout);
        }
    }

    private void BuildUI()
    {
        // ========== Input Editor ==========
        _inputEditor = MultiLineTextInput("json-input")
            .Title(Strings.InputTitle)
            .AlwaysShowLineNumber()
            .Language("json")
            .Extendable()
            .OnTextChanged(OnInputTextChangedDebounced);

        // ========== Output Editor (Beautify/Minify) ==========
        _outputEditor = MultiLineTextInput("json-output")
            .Title(Strings.OutputTitle)
            .ReadOnly()
            .AlwaysShowLineNumber()
            .Language("json")
            .Extendable();

        _beautifyMinifyPanel = Grid()
            .Rows(Fraction(1))
            .Columns(Fraction(1))
            .Cells(Cell(0, 0, 1, 1, _outputEditor));

        // ========== Tree View ==========
        _treeViewEditor = MultiLineTextInput("tree-output")
            .Title(Strings.ModeTreeView)
            .ReadOnly()
            .AlwaysShowLineNumber()
            .Extendable();

        _treePathLabel = Label("tree-path").Text("—");
        _treeValueLabel = Label("tree-value").Text("—");

        _treeViewPanel = Grid()
            .Rows(Fraction(1), Auto)
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1, _treeViewEditor),
                Cell(1, 0, 1, 1,
                    Stack()
                        .Vertical()
                        .SmallSpacing()
                        .WithChildren(
                            Stack()
                                .Horizontal()
                                .SmallSpacing()
                                .WithChildren(
                                    Label().Text(Strings.SelectedNodePath + ":"),
                                    _treePathLabel
                                ),
                            Stack()
                                .Horizontal()
                                .SmallSpacing()
                                .WithChildren(
                                    Label().Text(Strings.SelectedNodeValue + ":"),
                                    _treeValueLabel
                                )
                        )
                )
            );

        // ========== Table View ==========
        _tableViewGrid = DataGrid("table-grid")
            .Extendable()
            .AllowSelectItem();

        _tableViewPanel = Grid()
            .Rows(Fraction(1))
            .Columns(Fraction(1))
            .Cells(Cell(0, 0, 1, 1, _tableViewGrid));

        // ========== Query ==========
        _queryLanguageSelector = SelectDropDownList("query-lang")
            .WithItems(QueryLanguageItems)
            .OnItemSelected(OnQueryLanguageItemSelected);

        _queryExpressionInput = SingleLineTextInput("query-expr")
            .Title(Strings.QueryExpressionTitle)
            .OnTextChanged(OnQueryExpressionChanged);

        _queryResultEditor = MultiLineTextInput("query-result")
            .Title(Strings.QueryResultTitle)
            .ReadOnly()
            .AlwaysShowLineNumber()
            .Language("json")
            .Extendable();

        _queryPanel = Grid()
            .Rows(Auto, Fraction(1))
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1,
                    Stack()
                        .Vertical()
                        .SmallSpacing()
                        .WithChildren(
                            Label().Text(Strings.QueryLanguageLabel),
                            _queryLanguageSelector,
                            _queryExpressionInput
                        )
                ),
                Cell(1, 0, 1, 1, _queryResultEditor)
            );

        // ========== Entity Conversion ==========
        _entityLanguageSelector = SelectDropDownList("entity-lang")
            .WithItems(EntityLanguageItems)
            .OnItemSelected(OnEntityLanguageItemSelected);

        _entityResultEditor = MultiLineTextInput("entity-result")
            .Title(Strings.GeneratedCodeTitle)
            .ReadOnly()
            .AlwaysShowLineNumber()
            .Extendable();

        _entityPanel = Grid()
            .Rows(Auto, Fraction(1))
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1,
                    Stack()
                        .Vertical()
                        .SmallSpacing()
                        .WithChildren(
                            Label().Text(Strings.EntityLanguageLabel),
                            _entityLanguageSelector
                        )
                ),
                Cell(1, 0, 1, 1, _entityResultEditor)
            );

        // ========== Sort ==========
        var sortAscButton = Button("sort-asc", Strings.SortAscendingButton)
            .OnClick(() => OnSortDirectionChanged(SortDirection.Ascending));
        var sortDescButton = Button("sort-desc", Strings.SortDescendingButton)
            .OnClick(() => OnSortDirectionChanged(SortDirection.Descending));

        _sortResultEditor = MultiLineTextInput("sort-result")
            .Title(Strings.SortedJsonTitle)
            .ReadOnly()
            .AlwaysShowLineNumber()
            .Language("json")
            .Extendable();

        _sortPanel = Grid()
            .Rows(Auto, Fraction(1))
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1,
                    Stack()
                        .Horizontal()
                        .SmallSpacing()
                        .WithChildren(sortAscButton, sortDescButton)
                ),
                Cell(1, 0, 1, 1, _sortResultEditor)
            );

        // Initially hide all panels except Beautify/Minify
        GUI.Hide(_treeViewPanel);
        GUI.Hide(_tableViewPanel);
        GUI.Hide(_queryPanel);
        GUI.Hide(_entityPanel);
        GUI.Hide(_sortPanel);

        // ========== Mode Buttons (replace dropdown) ==========
        var beautifyBtn = Button("mode-beautify", Strings.ModeBeautify)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.Beautify));
        var minifyBtn = Button("mode-minify", Strings.ModeMinify)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.Minify));
        var treeViewBtn = Button("mode-tree", Strings.ModeTreeView)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.TreeView));
        var tableViewBtn = Button("mode-table", Strings.ModeTableView)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.TableView));
        var queryBtn = Button("mode-query", Strings.ModeQuery)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.Query));
        var entityBtn = Button("mode-entity", Strings.ModeEntityConvert)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.EntityConvert));
        var sortBtn = Button("mode-sort", Strings.ModeSort)
            .OnClick(() => OnModeButtonClicked(JsonProcessingMode.Sort));

        _modeButtonBar = Stack()
            .Horizontal()
            .SmallSpacing()
            .WithChildren(
                beautifyBtn,
                minifyBtn,
                treeViewBtn,
                tableViewBtn,
                queryBtn,
                entityBtn,
                sortBtn
            );

        // ========== Right pane output area (all panels overlay in a single Grid cell) ==========
        // Each panel fills the entire cell; only one is visible at a time via Show/Hide.
        var outputArea = Grid()
            .Rows(Fraction(1))
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1, _beautifyMinifyPanel),
                Cell(0, 0, 1, 1, _treeViewPanel),
                Cell(0, 0, 1, 1, _tableViewPanel),
                Cell(0, 0, 1, 1, _queryPanel),
                Cell(0, 0, 1, 1, _entityPanel),
                Cell(0, 0, 1, 1, _sortPanel)
            );

        // ========== Main Layout: Grid (Row0=mode buttons Auto, Row1=SplitGrid Fraction) ==========
        // This follows the official DevToys pattern: Grid + Cell + SplitGrid
        _mainLayout = Grid()
            .Rows(Auto, Fraction(1))
            .Columns(Fraction(1))
            .Cells(
                Cell(0, 0, 1, 1, _modeButtonBar),
                Cell(1, 0, 1, 1,
                    SplitGrid()
                        .Vertical()
                        .LeftPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                        .RightPaneLength(new UIGridLength(1, UIGridUnitType.Fraction))
                        .WithLeftPaneChild(_inputEditor)
                        .WithRightPaneChild(outputArea)
                )
            );
    }

    // ==================== Debounced Input Handler ====================

    private async void OnInputTextChangedDebounced(string text)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();

        try
        {
            await Task.Delay(300, _debounceCts.Token);
            UpdateOutput();
        }
        catch (OperationCanceledException)
        {
        }
    }

    // ==================== Event Handlers ====================

    private ValueTask OnModeButtonClicked(JsonProcessingMode mode)
    {
        _currentMode = mode;
        SwitchOutputPanel();
        UpdateOutput();
        return ValueTask.CompletedTask;
    }

    private ValueTask OnQueryLanguageItemSelected(IUIDropDownListItem? item)
    {
        if (_currentMode == JsonProcessingMode.Query)
            UpdateOutput();
        return ValueTask.CompletedTask;
    }

    private void OnQueryExpressionChanged(string expression)
    {
        if (_currentMode == JsonProcessingMode.Query)
            UpdateOutput();
    }

    private ValueTask OnEntityLanguageItemSelected(IUIDropDownListItem? item)
    {
        if (_currentMode == JsonProcessingMode.EntityConvert)
            UpdateOutput();
        return ValueTask.CompletedTask;
    }

    private void OnSortDirectionChanged(SortDirection direction)
    {
        _sortDirection = direction;
        if (_currentMode == JsonProcessingMode.Sort)
            UpdateOutput();
    }

    // ==================== Output Panel Switching ====================

    private void SwitchOutputPanel()
    {
        GUI.Hide(_beautifyMinifyPanel);
        GUI.Hide(_treeViewPanel);
        GUI.Hide(_tableViewPanel);
        GUI.Hide(_queryPanel);
        GUI.Hide(_entityPanel);
        GUI.Hide(_sortPanel);

        switch (_currentMode)
        {
            case JsonProcessingMode.Beautify:
            case JsonProcessingMode.Minify:
                GUI.Show(_beautifyMinifyPanel);
                break;
            case JsonProcessingMode.TreeView:
                GUI.Show(_treeViewPanel);
                break;
            case JsonProcessingMode.TableView:
                GUI.Show(_tableViewPanel);
                break;
            case JsonProcessingMode.Query:
                GUI.Show(_queryPanel);
                break;
            case JsonProcessingMode.EntityConvert:
                GUI.Show(_entityPanel);
                break;
            case JsonProcessingMode.Sort:
                GUI.Show(_sortPanel);
                break;
        }
    }

    // ==================== Core Processing ====================

    private void UpdateOutput()
    {
        string input = _inputEditor.Text;

        if (string.IsNullOrWhiteSpace(input))
        {
            ClearAllOutputs();
            return;
        }

        // Validate JSON first
        var validationError = JsonProcessor.Validate(input);
        if (validationError != null)
        {
            string errorMsg = validationError.Message;
            if (validationError.Line > 0)
                errorMsg += $" ({Strings.LineLabel} {validationError.Line}, {Strings.ColumnLabel} {validationError.Column})";

            ShowErrorInOutput(errorMsg);
            return;
        }

        try
        {
            switch (_currentMode)
            {
                case JsonProcessingMode.Beautify:
                    _outputEditor.Text(JsonProcessor.Beautify(input));
                    break;

                case JsonProcessingMode.Minify:
                    _outputEditor.Text(JsonProcessor.Minify(input));
                    break;

                case JsonProcessingMode.TreeView:
                    UpdateTreeView(input);
                    break;

                case JsonProcessingMode.TableView:
                    UpdateTableView(input);
                    break;

                case JsonProcessingMode.Query:
                    ExecuteQuery(input);
                    break;

                case JsonProcessingMode.EntityConvert:
                    GenerateEntity(input);
                    break;

                case JsonProcessingMode.Sort:
                    UpdateSortOutput(input);
                    break;
            }
        }
        catch (Exception ex)
        {
            ShowErrorInOutput(ex.Message);
        }
    }

    private void ShowErrorInOutput(string errorMsg)
    {
        switch (_currentMode)
        {
            case JsonProcessingMode.Beautify:
            case JsonProcessingMode.Minify:
                _outputEditor.Text($"❌ {Strings.JsonParseError}\n\n{errorMsg}");
                break;
            case JsonProcessingMode.TreeView:
                _treeViewEditor.Text($"❌ {Strings.JsonParseError}\n\n{errorMsg}");
                break;
            case JsonProcessingMode.TableView:
                _tableViewGrid
                    .WithColumns(Strings.JsonParseError)
                    .WithRows(Row((object?)null, Cell(errorMsg)));
                break;
            case JsonProcessingMode.Query:
                _queryResultEditor.Text($"❌ {Strings.JsonParseError}\n\n{errorMsg}");
                break;
            case JsonProcessingMode.EntityConvert:
                _entityResultEditor.Text($"❌ {Strings.JsonParseError}\n\n{errorMsg}");
                break;
            case JsonProcessingMode.Sort:
                _sortResultEditor.Text($"❌ {Strings.JsonParseError}\n\n{errorMsg}");
                break;
        }
    }

    // ==================== Mode-specific Processing ====================

    private void UpdateTreeView(string json)
    {
        var tree = JsonProcessor.BuildTree(json);

        var sb = new StringBuilder();
        RenderTreeNode(sb, tree, 0, true);
        _treeViewEditor.Text(sb.ToString());

        _treePathLabel.Text(tree.Path ?? "$");
        _treeValueLabel.Text(Truncate(tree.RawValue, 200));
    }

    private void RenderTreeNode(StringBuilder sb, JsonTreeNode node, int indent, bool isLast)
    {
        string prefix = new string(' ', indent * 2);
        string connector = isLast ? "└── " : "├── ";
        string childPrefix = indent == 0 ? "" : connector;

        string typeTag = $"[{node.Type}]";
        string summary;

        if (node.Children.Count > 0)
        {
            summary = node.RawValue;
        }
        else
        {
            summary = node.Type is "String" ? $"\"{Truncate(node.RawValue, 50)}\""
                     : Truncate(node.RawValue, 50);
        }

        string namePart = string.IsNullOrEmpty(node.Name) || node.Name == "$"
            ? "$"
            : node.Name;

        sb.AppendLine($"{prefix}{childPrefix}{namePart} {typeTag} {summary}");

        for (int i = 0; i < node.Children.Count; i++)
        {
            bool childIsLast = i == node.Children.Count - 1;
            RenderTreeNode(sb, node.Children[i], indent + 1, childIsLast);
        }
    }

    private void UpdateTableView(string json)
    {
        string[]? columns = JsonProcessor.GetTableColumns(json);
        if (columns == null)
        {
            _tableViewGrid
                .WithColumns(Strings.InfoLabel)
                .WithRows(Row((object?)null, Cell(Strings.TableViewNotApplicable)));
            return;
        }

        var rows = JsonProcessor.GetTableRows(json, columns);
        if (rows == null)
            return;

        var dataGridRows = new List<IUIDataGridRow>();
        foreach (var rowDict in rows)
        {
            var cells = new List<IUIDataGridCell>();
            foreach (string col in columns)
            {
                string val = rowDict.GetValueOrDefault(col, "");
                cells.Add(Cell(val));
            }
            dataGridRows.Add(Row((object?)null, cells.ToArray()));
        }

        _tableViewGrid
            .WithColumns(columns)
            .WithRows(dataGridRows.ToArray());
    }

    private void ExecuteQuery(string json)
    {
        string expression = _queryExpressionInput.Text;
        if (string.IsNullOrWhiteSpace(expression))
        {
            _queryResultEditor.Text(string.Empty);
            return;
        }

        int langIndex = 0;
        var selItem = _queryLanguageSelector.SelectedItem;
        if (selItem != null)
            langIndex = Array.IndexOf(QueryLanguageItems, selItem);

        string result = langIndex == 0
            ? JsonQueryEngine.QueryJsonPath(json, expression)
            : JsonQueryEngine.QueryJmesPath(json, expression);

        _queryResultEditor.Text(string.IsNullOrEmpty(result) ? Strings.NoMatchesFound : result);
    }

    private void GenerateEntity(string json)
    {
        int langIndex = 0;
        var selItem = _entityLanguageSelector.SelectedItem;
        if (selItem != null)
            langIndex = Array.IndexOf(EntityLanguageItems, selItem);

        EntityLanguage lang = (EntityLanguage)langIndex;
        string code = EntityGenerator.Generate(json, lang);

        string languageName = lang switch
        {
            EntityLanguage.CSharp => "csharp",
            EntityLanguage.Java => "java",
            EntityLanguage.Python => "python",
            EntityLanguage.Go => "go",
            _ => ""
        };

        _entityResultEditor.Language(languageName);
        _entityResultEditor.Text(code);
    }

    private void UpdateSortOutput(string json)
    {
        string sorted = JsonProcessor.SortByKey(json, _sortDirection);
        _sortResultEditor.Text(sorted);
    }

    // ==================== Helpers ====================

    private void ClearAllOutputs()
    {
        _outputEditor.Text(string.Empty);
        _treeViewEditor.Text(string.Empty);
        _treePathLabel.Text("—");
        _treeValueLabel.Text("—");
        _queryResultEditor.Text(string.Empty);
        _entityResultEditor.Text(string.Empty);
        _sortResultEditor.Text(string.Empty);
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "…";
    }

    // ==================== IGuiTool ====================

    public void OnDataReceived(string dataTypeName, object? parsedData)
    {
        if (parsedData is string json)
        {
            _inputEditor.Text(json);
        }
    }
}
