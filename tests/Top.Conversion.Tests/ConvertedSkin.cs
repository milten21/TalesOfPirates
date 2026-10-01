using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Top.Conversion.Gltf;

namespace Top.Conversion.Tests
{
    /// <summary>
    /// Poses the skin of a converted file as a glTF viewer does: each node at
    /// its rest, or at one frame of the file's clip, times its parents, and
    /// each joint's inverse bind in front. Row-vector form.
    /// </summary>
    internal class ConvertedSkin
    {
        private readonly GltfDocument _document;
        private readonly int[] _parents;
        private readonly Matrix4x4[] _inverseBinds;
        private readonly Dictionary<(int Node, string Path), float[]> _channels =
            new Dictionary<(int Node, string Path), float[]>();

        internal ConvertedSkin(GltfFile file)
        {
            var data = new GltfData(file);
            var skin = file.Document.Skins.Single();
            var inverseBinds = data.ReadFloats(skin.InverseBindMatrices.Value);

            _document = file.Document;
            _parents = Enumerable.Repeat(-1, _document.Nodes.Count).ToArray();
            _inverseBinds = skin.Joints.Select((_, j) => Matrix(inverseBinds, j * 16)).ToArray();

            for (var i = 0; i < _document.Nodes.Count; i++)
            {
                foreach (var child in _document.Nodes[i].Children ?? new List<int>())
                {
                    _parents[child] = i;
                }
            }

            if (_document.Animations == null)
            {
                return;
            }

            var animation = _document.Animations.Single();

            FrameCount = data.ReadFloats(animation.Samplers[0].Input).Length;

            foreach (var channel in animation.Channels)
            {
                _channels[(channel.Target.Node.Value, channel.Target.Path)] =
                    data.ReadFloats(animation.Samplers[channel.Sampler].Output);
            }
        }

        internal int FrameCount { get; }

        /// <summary>
        /// Each joint's world matrix times its inverse bind, at rest when
        /// <paramref name="frame"/> is null.
        /// </summary>
        internal Matrix4x4[] At(int? frame)
        {
            return _document.Skins.Single().Joints
                .Select((joint, j) => _inverseBinds[j] * World(joint, frame))
                .ToArray();
        }

        private static Matrix4x4 Matrix(float[] columnMajor, int offset)
        {
            var matrix = new Matrix4x4();

            for (var i = 0; i < 16; i++)
            {
                matrix[i / 4, i % 4] = columnMajor[offset + i];
            }

            return matrix;
        }

        private Matrix4x4 World(int node, int? frame)
        {
            var local = Local(node, frame);

            return _parents[node] < 0 ? local : local * World(_parents[node], frame);
        }

        private Matrix4x4 Local(int node, int? frame)
        {
            var gltfNode = _document.Nodes[node];

            if (gltfNode.Matrix != null)
            {
                return Matrix(gltfNode.Matrix, 0);
            }

            var translation = Keyed(node, "translation", frame) ?? gltfNode.Translation ?? [0f, 0f, 0f];
            var rotation = Keyed(node, "rotation", frame) ?? gltfNode.Rotation ?? [0f, 0f, 0f, 1f];
            var scale = Keyed(node, "scale", frame) ?? gltfNode.Scale ?? [1f, 1f, 1f];

            return Matrix4x4.CreateScale(scale[0], scale[1], scale[2])
                   * Matrix4x4.CreateFromQuaternion(new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]))
                   * Matrix4x4.CreateTranslation(translation[0], translation[1], translation[2]);
        }

        private float[] Keyed(int node, string path, int? frame)
        {
            if (frame is not { } index || !_channels.TryGetValue((node, path), out var values))
            {
                return null;
            }

            var width = path == "rotation" ? 4 : 3;

            return values.Skip(index * width).Take(width).ToArray();
        }
    }
}
