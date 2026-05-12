using DevToys.Api;
using System.ComponentModel.Composition;

namespace DevToys.JsonPowerTool;

[Export(typeof(IResourceAssemblyIdentifier))]
[Name(nameof(JsonPowerToolResourceAssemblyIdentifier))]
internal sealed class JsonPowerToolResourceAssemblyIdentifier : IResourceAssemblyIdentifier
{
    public ValueTask<FontDefinition[]> GetFontDefinitionsAsync()
    {
        return new ValueTask<FontDefinition[]>(Array.Empty<FontDefinition>());
    }
}
