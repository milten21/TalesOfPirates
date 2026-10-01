using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using Top.Client.Rendering.Models.Gltf;
using Top.Logging;

namespace Top.Client.Rendering.Models;

public class ModelFactory(string contentFolder)
{
    private const uint NamedSkinBindsFlag = 16;

    private static GltfDocumentExtension[]? _extensions;

    private readonly Dictionary<(string ContentPath, bool Autoplay), ImportedModel?> _models =
        new Dictionary<(string ContentPath, bool Autoplay), ImportedModel?>();

    private readonly Dictionary<string, AnimationLibrary?> _animations = new Dictionary<string, AnimationLibrary?>();

    public static void RegisterExtensions()
    {
        if (_extensions != null)
        {
            return;
        }

        _extensions =
        [
            new TopMaterialExtension(),
            new TopNamingExtension(),
            new TopAnimationExtension()
        ];

        foreach (var extension in _extensions)
        {
            GltfDocument.RegisterGltfDocumentExtension(extension);
        }
    }

    public static void UnregisterExtensions()
    {
        if (_extensions == null)
        {
            return;
        }

        foreach (var extension in _extensions)
        {
            GltfDocument.UnregisterGltfDocumentExtension(extension);
        }

        _extensions = null;
    }

    public Node3D? Instantiate(string contentPath)
    {
        return ResolveModel(contentPath, autoplay: true)?.Scene.Instantiate<Node3D>();
    }

    public Node3D? InstantiateCharacter(string contentPath)
    {
        return ResolveModel(contentPath, autoplay: false)?.Scene.Instantiate<Node3D>();
    }

    public RigInstance? InstantiateRig(string contentPath)
    {
        if (ResolveModel(contentPath, autoplay: false) is not { } model)
        {
            return null;
        }

        if (model.Skins.Count != 1)
        {
            Log.Error($"{contentPath} carries {model.Skins.Count} skins, a rig must carry exactly one");

            return null;
        }

        return new RigInstance(model.Scene.Instantiate<Node3D>(), model.Skins[0]);
    }

    public void Preload(string contentPath)
    {
        ResolveModel(contentPath, autoplay: true);
    }

    public Animation? LoadAnimation(string contentPath, string animationName)
    {
        if (!_animations.TryGetValue(contentPath, out var library))
        {
            library = ImportAnimations(contentPath);
            _animations[contentPath] = library;
        }

        return library != null && library.HasAnimation(animationName) ? library.GetAnimation(animationName) : null;
    }

    private static IReadOnlyList<Skin> ReadSkins(GltfState state)
    {
        return state.GetSkins().Select(skin => skin.GodotSkin).ToList();
    }

    private ImportedModel? ResolveModel(string contentPath, bool autoplay)
    {
        if (_models.TryGetValue((contentPath, autoplay), out var known))
        {
            return known;
        }

        var model = ImportModel(contentPath, autoplay);

        _models[(contentPath, autoplay)] = model;

        return model;
    }

    private ImportedModel? ImportModel(string contentPath, bool autoplay)
    {
        var state = new GltfState();

        TopAnimationExtension.SetAutoplay(state, autoplay);

        if (GenerateScene(contentPath, state) is not { } root)
        {
            return null;
        }

        var packed = new PackedScene();
        var packError = packed.Pack(root);

        root.Free();

        if (packError == Error.Ok)
        {
            return new ImportedModel(packed, ReadSkins(state));
        }

        Log.Error($"{contentPath} did not pack: {packError}");

        return null;
    }

    private AnimationLibrary? ImportAnimations(string contentPath)
    {
        var state = new GltfState();

        TopAnimationExtension.SetAutoplay(state, false);

        if (GenerateScene(contentPath, state) is not { } root)
        {
            return null;
        }

        var library = new AnimationLibrary();

        foreach (var player in root.GetChildren().OfType<AnimationPlayer>())
        {
            foreach (var animationName in player.GetAnimationList())
            {
                library.AddAnimation(animationName, player.GetAnimation(animationName));
            }
        }

        root.Free();

        return library;
    }

    private Node3D? GenerateScene(string contentPath, GltfState state)
    {
        RegisterExtensions();

        var path = Path.Combine(contentFolder, contentPath.Replace('/', Path.DirectorySeparatorChar));
        var document = new GltfDocument();
        var error = document.AppendFromFile(path, state, NamedSkinBindsFlag);

        if (error != Error.Ok)
        {
            Log.Error($"{path} did not parse: {error}");

            return null;
        }

        if (document.GenerateScene(state, removeImmutableTracks: false) is not Node3D root)
        {
            Log.Error($"{path} did not instantiate");

            return null;
        }

        return root;
    }

    private record ImportedModel(PackedScene Scene, IReadOnlyList<Skin> Skins);
}
