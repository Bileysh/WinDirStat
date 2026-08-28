using System.Text.Json.Serialization;
using WinDirStat.Core.Entities;

namespace WinDirStat_App.Services;

[JsonSerializable(typeof(FileSystemNode))]
[JsonSerializable(typeof(Dictionary<string, FileSystemNode>))]
internal partial class FileSystemNodeJsonContext : JsonSerializerContext;
