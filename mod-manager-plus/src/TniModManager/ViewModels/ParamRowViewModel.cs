using CommunityToolkit.Mvvm.ComponentModel;
using TniModManager.Core.Models;

namespace TniModManager.ViewModels;

public partial class ParamRowViewModel : ObservableObject
{
    public string Name { get; init; } = "";
    public string Label { get; init; } = "";
    public string Type { get; init; } = "string";
    public string Description { get; init; } = "";
    public List<string> Options { get; init; } = [];

    [ObservableProperty] private bool _boolValue;
    [ObservableProperty] private string _textValue = "";
    [ObservableProperty] private string _selectedOption = "";

    public bool IsBoolean => Type == "boolean";
    public bool IsSelect => Type == "select";
    public bool IsText => Type is not ("boolean" or "select");

    public static ParamRowViewModel FromDef(ParameterDef def, object? current)
    {
        var row = new ParamRowViewModel
        {
            Name = def.Name,
            Label = string.IsNullOrEmpty(def.Label) ? def.Name : def.Label,
            Type = def.Type.ToLowerInvariant(),
            Description = def.Description,
            Options = def.Options ?? []
        };

        var value = current ?? (def.Default.HasValue ? JsonElementToObject(def.Default.Value) : null);
        switch (row.Type)
        {
            case "boolean":
                row.BoolValue = value is true or "true";
                break;
            case "select":
                row.SelectedOption = value?.ToString() ?? row.Options.FirstOrDefault() ?? "";
                break;
            default:
                row.TextValue = value?.ToString() ?? "";
                break;
        }
        return row;
    }

    public object? GetValue() => Type switch
    {
        "boolean" => BoolValue,
        "select" => SelectedOption,
        "integer" when int.TryParse(TextValue, out var i) => i,
        "number" when double.TryParse(TextValue, out var d) => d,
        _ => TextValue
    };

    private static object? JsonElementToObject(System.Text.Json.JsonElement el) => el.ValueKind switch
    {
        System.Text.Json.JsonValueKind.True => true,
        System.Text.Json.JsonValueKind.False => false,
        System.Text.Json.JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        System.Text.Json.JsonValueKind.String => el.GetString(),
        _ => el.ToString()
    };
}
