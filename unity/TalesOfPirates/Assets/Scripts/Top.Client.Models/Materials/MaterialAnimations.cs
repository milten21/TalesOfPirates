using System;
using System.Collections.Generic;
using GLTFast;
using Top.Client.Models.Animations;
using Top.Client.Core;
using Top.Contracts.Assets.Models.Extras;
using UnityEngine;

namespace Top.Client.Models.Materials
{
    public class MaterialAnimations : IDisposable
    {
        private class Tracks
        {
            public UvAnimationTrack Uv;
            public OpacityAnimationTrack Opacity;
            public FlipbookTrack Flipbook;
        }

        private readonly IGltfReadable _gltf;

        private readonly Dictionary<int, Tracks> _perMaterial = new Dictionary<int, Tracks>();

        public MaterialAnimations(IGltfReadable gltf)
        {
            _gltf = gltf;
        }

        public void Attach(Renderer renderer, int[] materialIndices)
        {
            for (var slot = 0; slot < materialIndices.Length; slot++)
            {
                var tracks = TracksFor(materialIndices[slot]);

                if (tracks == null)
                {
                    continue;
                }

                if (tracks.Uv != null)
                {
                    var animation = renderer.gameObject.AddComponent<UvAnimation>();

                    animation.materialIndex = slot;
                    animation.track = tracks.Uv;
                }

                if (tracks.Opacity != null)
                {
                    var animation = renderer.gameObject.AddComponent<OpacityAnimation>();

                    animation.materialIndex = slot;
                    animation.track = tracks.Opacity;
                }

                if (tracks.Flipbook != null)
                {
                    var animation = renderer.gameObject.AddComponent<FlipbookAnimation>();

                    animation.materialIndex = slot;
                    animation.track = tracks.Flipbook;
                }
            }
        }

        public void Dispose()
        {
            foreach (var tracks in _perMaterial.Values)
            {
                if (tracks == null)
                {
                    continue;
                }

                UnityObjects.Destroy(tracks.Uv);
                UnityObjects.Destroy(tracks.Opacity);
                UnityObjects.Destroy(tracks.Flipbook);
            }

            _perMaterial.Clear();
        }

        private Tracks TracksFor(int material)
        {
            if (material < 0)
            {
                return null;
            }

            if (_perMaterial.TryGetValue(material, out var tracks))
            {
                return tracks;
            }

            tracks = MaterialStateReader.TryReadExtras(_gltf.GetSourceMaterial(material), out var extras)
                ? Build(extras)
                : null;

            _perMaterial[material] = tracks;

            return tracks;
        }

        private Tracks Build(MaterialExtras extras)
        {
            if (extras.UvAnimation == null && extras.OpacityAnimation == null && extras.Flipbook == null)
            {
                return null;
            }

            return new Tracks
            {
                Uv = extras.UvAnimation != null ? Kept(TrackMapper.CreateUvTrack(extras.UvAnimation)) : null,
                Opacity = extras.OpacityAnimation != null
                    ? Kept(TrackMapper.CreateOpacityTrack(extras.OpacityAnimation))
                    : null,
                Flipbook = extras.Flipbook != null
                    ? Kept(TrackMapper.CreateFlipbookTrack(extras.Flipbook, _gltf.GetTexture))
                    : null,
            };
        }

        private static T Kept<T>(T track) where T : ScriptableObject
        {
            track.hideFlags = HideFlags.HideAndDontSave;

            return track;
        }
    }
}
