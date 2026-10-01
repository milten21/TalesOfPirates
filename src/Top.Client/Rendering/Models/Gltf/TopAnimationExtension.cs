using System.Collections.Generic;
using Godot;

namespace Top.Client.Rendering.Models.Gltf;

[Tool]
public partial class TopAnimationExtension : GltfDocumentExtension
{
    public const string AnimationPlayerName = "AnimationPlayer";

    private const string AutoplayKey = "TOP_autoplay";

    private class AnimationGroup
    {
        public Node Host = null!;

        public readonly List<Animation> Animations = [];
    }

    public static void SetAutoplay(GltfState state, bool autoplay)
    {
        state.SetAdditionalData(AutoplayKey, autoplay);
    }

    public override Error _ImportPostParse(GltfState state)
    {
        var autoplay = ReadAutoplay(state);

        foreach (var animation in state.Animations)
        {
            animation.Loop = autoplay;
        }

        return Error.Ok;
    }

    public override Error _ImportPost(GltfState state, Node root)
    {
        if (state.Animations.Count == 0)
        {
            return Error.Ok;
        }

        var imported = FindPlayer(root);

        if (imported == null)
        {
            return Error.Ok;
        }

        var groups = GroupByHost(state, root, imported);
        var autoplay = ReadAutoplay(state);

        root.RemoveChild(imported);

        foreach (var group in groups)
        {
            AddPlayer(root, group, autoplay);
        }

        imported.Free();

        return Error.Ok;
    }

    private static bool ReadAutoplay(GltfState state)
    {
        var stored = state.GetAdditionalData(AutoplayKey);

        return stored.VariantType == Variant.Type.Nil || stored.AsBool();
    }

    private static AnimationPlayer? FindPlayer(Node root)
    {
        foreach (var child in root.GetChildren())
        {
            if (child is AnimationPlayer player)
            {
                return player;
            }
        }

        return null;
    }

    private static List<AnimationGroup> GroupByHost(GltfState state, Node root, AnimationPlayer imported)
    {
        var groups = new List<AnimationGroup>();

        foreach (var described in state.Animations)
        {
            var name = described.ResourceName;

            if (!imported.HasAnimation(name))
            {
                continue;
            }

            var animation = imported.GetAnimation(name);
            var host = FindHost(root, animation);
            var group = groups.Find(candidate => candidate.Host == host);

            if (group == null)
            {
                group = new AnimationGroup { Host = host };
                groups.Add(group);
            }

            group.Animations.Add(animation);
        }

        return groups;
    }

    private static void AddPlayer(Node root, AnimationGroup group, bool autoplay)
    {
        var player = new AnimationPlayer { Name = AnimationPlayerName };
        var library = new AnimationLibrary();

        foreach (var animation in group.Animations)
        {
            library.AddAnimation(animation.ResourceName, animation);
        }

        player.AddAnimationLibrary("", library);
        root.AddChild(player, forceReadableName: true);
        player.Owner = root;

        if (!autoplay)
        {
            return;
        }

        var started = group.Animations.Find(animation => animation.GetTrackCount() > 0);

        if (started != null)
        {
            player.Autoplay = started.ResourceName;
        }
    }

    private static Node FindHost(Node root, Animation animation)
    {
        Node? host = null;

        for (var track = 0; track < animation.GetTrackCount(); track++)
        {
            var names = animation.TrackGetPath(track).GetConcatenatedNames();
            var target = string.IsNullOrEmpty(names) ? root : root.GetNodeOrNull(names);

            if (target == null)
            {
                continue;
            }

            host = host == null ? target : FindCommonAncestor(root, host, target);
        }

        return host ?? root;
    }

    private static Node FindCommonAncestor(Node root, Node first, Node second)
    {
        var chain = new HashSet<Node>();

        for (var node = first; node != null; node = node == root ? null : node.GetParent())
        {
            chain.Add(node);
        }

        for (var node = second; node != null; node = node == root ? null : node.GetParent())
        {
            if (chain.Contains(node))
            {
                return node;
            }
        }

        return root;
    }
}
