#if TOOLS
using System;
using System.Collections.Generic;
using Godot;
using Top.Client.Rendering.Models.Gltf;

namespace Top.Client.Addons.TopGltf;

public static class StaleGltfCollector
{
    public static string[] Find(EditorFileSystemDirectory root)
    {
        var stale = new List<string>();

        Collect(root, stale);

        return stale.ToArray();
    }

    private static void Collect(EditorFileSystemDirectory directory, List<string> stale)
    {
        for (var i = 0; i < directory.GetFileCount(); i++)
        {
            var path = directory.GetFilePath(i);

            if (IsGltf(path) && directory.GetFileType(i) == nameof(PackedScene) && IsStale(path))
            {
                stale.Add(path);
            }
        }

        for (var i = 0; i < directory.GetSubdirCount(); i++)
        {
            Collect(directory.GetSubdir(i), stale);
        }
    }

    private static bool IsGltf(string path)
    {
        return path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase)
               || path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStale(string path)
    {
        var imported = ResourceLoader.Load(path, nameof(PackedScene), ResourceLoader.CacheMode.Ignore);

        return imported is not PackedScene scene || !TopImportMarker.IsStamped(scene);
    }
}
#endif
