using System;
using System.Collections.Generic;
using Godot;
using Top.Contracts.Assets.Models;
using Top.Contracts.Assets.Models.Extras;

namespace Top.Client.Rendering.Models.Animations;

public static class MaterialTracks
{
    private const string AnimationName = "materials";

    public static void AddPlayers(Node root, Godot.Collections.Array<Material> materials,
        IReadOnlyList<MaterialExtras?> extras, Func<int, Texture2D?> resolveTexture)
    {
        var surfaces = FindSurfacePaths(root, materials);
        var animations = new Dictionary<int, Animation>();

        for (var i = 0; i < materials.Count && i < extras.Count; i++)
        {
            if (!surfaces.TryGetValue(i, out var path) || extras[i] is not { } described)
            {
                continue;
            }

            AddUvTracks(animations, path, described.UvAnimation);
            AddOpacityTrack(animations, path, described.OpacityAnimation);
            AddFlipbookTrack(animations, path, described.Flipbook, resolveTexture);
        }

        foreach (var animation in animations.Values)
        {
            if (animation.GetTrackCount() > 0)
            {
                AddPlayer(root, animation);
            }
        }
    }

    private static Dictionary<int, string> FindSurfacePaths(Node root,
        Godot.Collections.Array<Material> materials)
    {
        var surfaces = new Dictionary<int, string>();

        foreach (var node in Walk(root))
        {
            for (var surface = 0; surface < CountSurfaces(node); surface++)
            {
                var index = ReadSurfaceMaterial(node, surface) is { } material
                    ? materials.IndexOf(material)
                    : -1;

                if (index >= 0 && !surfaces.ContainsKey(index))
                {
                    surfaces[index] = $"{root.GetPathTo(node)}:mesh:surface_{surface}/material";
                }
            }
        }

        return surfaces;
    }

    private static int CountSurfaces(Node node)
    {
        return node switch
        {
            MeshInstance3D instance => instance.Mesh?.GetSurfaceCount() ?? 0,
            ImporterMeshInstance3D imported => imported.Mesh?.GetSurfaceCount() ?? 0,
            _ => 0,
        };
    }

    private static Material? ReadSurfaceMaterial(Node node, int surface)
    {
        return node switch
        {
            MeshInstance3D instance => instance.Mesh.SurfaceGetMaterial(surface),
            ImporterMeshInstance3D imported => imported.Mesh.GetSurfaceMaterial(surface),
            _ => null,
        };
    }

    private static void AddUvTracks(Dictionary<int, Animation> animations, string path,
        UvAnimationExtras? uv)
    {
        var frames = uv?.Frames;

        if (uv == null || frames == null || frames.Length == 0)
        {
            return;
        }

        var rate = ResolveRate(uv.FramesPerSecond);
        var length = frames.Length / rate;
        var animation = ResolveAnimation(animations, length);
        var transforms = AddTrack(animation, $"{path}:shader_parameter/uv_transform");
        var offsets = AddTrack(animation, $"{path}:shader_parameter/uv_offset");
        var transform = new Vector4(1f, 0f, 0f, 1f);
        var offset = Vector2.Zero;

        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i] is { Length: >= 6 } frame)
            {
                transform = new Vector4(frame[0], frame[1], frame[2], frame[3]);
                offset = new Vector2(frame[4], frame[5]);
            }
            else
            {
                transform = new Vector4(1f, 0f, 0f, 1f);
                offset = Vector2.Zero;
            }

            animation.TrackInsertKey(transforms, i / rate, transform);
            animation.TrackInsertKey(offsets, i / rate, offset);
        }

        animation.TrackInsertKey(transforms, length, transform);
        animation.TrackInsertKey(offsets, length, offset);
    }

    private static void AddOpacityTrack(Dictionary<int, Animation> animations, string path,
        OpacityAnimationExtras? opacity)
    {
        var keyFrames = opacity?.KeyFrames;
        var values = opacity?.Values;

        if (opacity == null || keyFrames == null || values == null || keyFrames.Length == 0
            || keyFrames.Length != values.Length)
        {
            return;
        }

        var rate = ResolveRate(opacity.FramesPerSecond);
        var length = (keyFrames[keyFrames.Length - 1] + 1) / rate;
        var animation = ResolveAnimation(animations, length);
        var track = AddTrack(animation, $"{path}:shader_parameter/opacity");

        for (var i = 0; i < keyFrames.Length; i++)
        {
            animation.TrackInsertKey(track, keyFrames[i] / rate, values[i]);
        }

        animation.TrackInsertKey(track, length, values[values.Length - 1]);
    }

    private static void AddFlipbookTrack(Dictionary<int, Animation> animations, string path,
        FlipbookExtras? flipbook, Func<int, Texture2D?> resolveTexture)
    {
        var frames = flipbook?.Frames;

        if (flipbook == null || frames == null || frames.Length == 0)
        {
            return;
        }

        var rate = ResolveRate(flipbook.FramesPerSecond);
        var length = frames.Length / rate;
        var animation = ResolveAnimation(animations, length);
        var track = AddTrack(animation, $"{path}:shader_parameter/base_map");

        animation.ValueTrackSetUpdateMode(track, Animation.UpdateMode.Discrete);

        for (var i = 0; i < frames.Length; i++)
        {
            var texture = resolveTexture(frames[i]);

            if (texture != null)
            {
                animation.TrackInsertKey(track, i / rate, texture);
            }
        }

        if (animation.TrackGetKeyCount(track) == 0)
        {
            animation.RemoveTrack(track);
        }
    }

    private static void AddPlayer(Node root, Animation animation)
    {
        var library = new AnimationLibrary();
        var player = new AnimationPlayer { Name = "MaterialAnimationPlayer" };

        library.AddAnimation(AnimationName, animation);
        player.AddAnimationLibrary("", library);
        root.AddChild(player, forceReadableName: true);
        player.Owner = root;
        player.Autoplay = AnimationName;
    }

    private static Animation ResolveAnimation(Dictionary<int, Animation> animations, float length)
    {
        var key = Mathf.RoundToInt(length * 1000f);

        if (animations.TryGetValue(key, out var known))
        {
            return known;
        }

        var animation = new Animation
        {
            Length = length,
            LoopMode = Animation.LoopModeEnum.Linear,
        };

        animations[key] = animation;

        return animation;
    }

    private static int AddTrack(Animation animation, string path)
    {
        var track = animation.AddTrack(Animation.TrackType.Value);

        animation.TrackSetPath(track, path);

        return track;
    }

    private static float ResolveRate(float framesPerSecond)
    {
        return framesPerSecond > 0f ? framesPerSecond : AnimationRate.FramesPerSecond;
    }

    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;

        foreach (var child in node.GetChildren())
        {
            foreach (var found in Walk(child))
            {
                yield return found;
            }
        }
    }
}
