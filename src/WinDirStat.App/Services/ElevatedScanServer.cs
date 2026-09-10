using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using WinDirStat.Core.Entities;
using WinDirStat.Services;

namespace WinDirStat_App.Services;

public static class ElevatedScanServer
{
    public static int Run(string inputFile, string outputFile)
    {
        try
        {
            var paths = File.ReadAllLines(inputFile);
            var results = new Dictionary<string, FileSystemNode>();
            
            var identityService = new FileIdentityService();
            var scanService = new DiskScanService(identityService);
            
            foreach (var path in paths)
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    var result = scanService.ScanAsync(path, accountForHardLinks: true).GetAwaiter().GetResult();
                    results[path] = result.RootNode;
                }
            }
            
            var json = JsonSerializer.Serialize(results, FileSystemNodeJsonContext.Default.DictionaryStringFileSystemNode);
            File.WriteAllText(outputFile, json);
            
            return 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ElevatedScanServer] Fatal error: {ex}");
            return -1;
        }
    }
}