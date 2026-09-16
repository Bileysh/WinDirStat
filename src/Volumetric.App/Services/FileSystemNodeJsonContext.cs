using System.Text.Json.Serialization;
using Volumetric.Core.Entities;

namespace Volumetric_App.Services;

[JsonSerializable(typeof(FileSystemNode))]
[JsonSerializable(typeof(Dictionary<string, FileSystemNode>))]
internal partial class FileSystemNodeJsonContext : JsonSerializerContext;
