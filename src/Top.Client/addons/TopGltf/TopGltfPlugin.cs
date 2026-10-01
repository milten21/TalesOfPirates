#if TOOLS
using Godot;
using Top.Client.Core;
using Top.Client.Rendering.Models;
using Top.Logging;

namespace Top.Client.Addons.TopGltf;

[Tool]
public partial class TopGltfPlugin : EditorPlugin
{
    private readonly Callable _repair;

    public TopGltfPlugin()
    {
        _repair = Callable.From(RepairStaleImports);
    }

    private static EditorFileSystem Filesystem => EditorInterface.Singleton.GetResourceFilesystem();

    public override void _EnterTree()
    {
        Log.Writer ??= new GodotLogWriter();

        ModelFactory.RegisterExtensions();

        _repair.CallDeferred();
    }

    public override void _ExitTree()
    {
        StopWaitingForScan();

        ModelFactory.UnregisterExtensions();
    }

    private void RepairStaleImports()
    {
        if (!IsInsideTree())
        {
            return;
        }

        var filesystem = Filesystem;

        if (filesystem.IsScanning() || filesystem.GetFilesystem() is not { } root || IsEmpty(root))
        {
            WaitForScan();

            return;
        }

        StopWaitingForScan();

        var stale = StaleGltfCollector.Find(root);

        if (stale.Length == 0)
        {
            return;
        }

        Log.Info($"Reimporting {stale.Length} glTF file(s) imported without the TOP extensions: {string.Join(", ", stale)}");

        filesystem.ReimportFiles(stale);
    }

    private static bool IsEmpty(EditorFileSystemDirectory root)
    {
        return root.GetFileCount() == 0 && root.GetSubdirCount() == 0;
    }

    private void WaitForScan()
    {
        if (!Filesystem.IsConnected(EditorFileSystem.SignalName.FilesystemChanged, _repair))
        {
            Filesystem.Connect(EditorFileSystem.SignalName.FilesystemChanged, _repair);
        }
    }

    private void StopWaitingForScan()
    {
        if (Filesystem.IsConnected(EditorFileSystem.SignalName.FilesystemChanged, _repair))
        {
            Filesystem.Disconnect(EditorFileSystem.SignalName.FilesystemChanged, _repair);
        }
    }
}
#endif
