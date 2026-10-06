using System.Text.Json.Serialization;
using Volumetric.Core.Entities;

namespace Volumetric_App.Services;

[JsonSourceGenerationOptions(MaxDepth = MaxTreeJsonDepth)]
[JsonSerializable(typeof(FileSystemNode))]
[JsonSerializable(typeof(Dictionary<string, FileSystemNode>))]
internal partial class FileSystemNodeJsonContext : JsonSerializerContext
{
    public const int MaxTreeJsonDepth = 32768;
}
