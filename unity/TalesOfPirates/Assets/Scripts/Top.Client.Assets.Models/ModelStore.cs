using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using Top.Client.Assets.Models.Gltf;
using Top.Client.Assets.Models.Materials;
using Top.Client.Core;
using Top.Content.Packs;
using Top.Logging;
using UnityEngine;
using GltfImport = GLTFast.Newtonsoft.GltfImport;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// The <c>ModelStore</c> class manages the lifecycle of models by supporting
    /// loading, instantiation, and releasing of model assets. It provides mechanisms
    /// to preload models for optimized runtime operations and ensures proper resource
    /// management.
    /// </summary>
    public class ModelStore : IModelStore
    {
        private class Entry
        {
            public Task Loading;
            public GltfImport Import;
            public List<HostedClip> Clips;
            public MaterialAnimations Materials;
            public int Instances;
        }

        private static readonly InstantiationSettings Settings = new InstantiationSettings
        {
            SceneObjectCreation = SceneObjectCreation.Always,
        };

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>(StringComparer.Ordinal);

        private readonly IComposedContent _content;

        public ModelStore(IComposedContent content)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
        }

        /// <summary>
        /// Builds the model at <paramref name="path"/> under
        /// <paramref name="parent"/>, returning it only once it is whole.
        /// </summary>
        /// <remarks>
        /// The instance count rises before instantiation, not after, so a
        /// release running in between cannot take away the import this spawn is
        /// still building from.
        /// </remarks>
        public async Task<ModelInstance> Spawn(string path, Transform parent,
            CancellationToken cancellationToken = default)
        {
            var entry = await Ready(path);

            cancellationToken.ThrowIfCancellationRequested();

            var instantiator = new Instantiator(entry.Import, parent, entry.Materials, settings: Settings);
            var built = false;

            entry.Instances++;

            try
            {
                if (!await entry.Import.InstantiateMainSceneAsync(instantiator, cancellationToken))
                {
                    throw new InvalidOperationException($"{path} did not instantiate");
                }

                Play(instantiator, entry.Clips);

                built = true;

                return new ModelInstance(this, path, instantiator.Scene);
            }
            finally
            {
                if (!built)
                {
                    if (instantiator.Scene != null)
                    {
                        UnityObjects.Destroy(instantiator.Scene.gameObject);
                    }

                    Release(path);
                }
            }
        }

        /// <summary>
        /// Imports the given models without instantiating any of them, so a
        /// later spawn has nothing left to wait for.
        /// </summary>
        public Task Preload(IEnumerable<string> paths)
        {
            var loads = paths.Select(Ready).Cast<Task>().ToList();

            return Task.WhenAll(loads);
        }

        internal void Release(string path)
        {
            if (!_entries.TryGetValue(path, out var entry))
            {
                return;
            }

            entry.Instances--;

            if (entry.Instances > 0)
            {
                return;
            }

            _entries.Remove(path);
            entry.Materials.Destroy();
            entry.Import.Dispose();

            foreach (var hosted in entry.Clips)
            {
                UnityObjects.Destroy(hosted.Clip);
            }
        }

        /// <remarks>
        /// Concurrent spawns for one path await the one load. A load that broke
        /// vacates its entry, so the next spawn tries again instead of handing
        /// out the same failure forever.
        /// </remarks>
        private async Task<Entry> Ready(string path)
        {
            if (!_entries.TryGetValue(path, out var entry))
            {
                entry = new Entry();
                _entries[path] = entry;
                entry.Loading = Load(path, entry);
            }

            try
            {
                await entry.Loading;
            }
            catch
            {
                Vacate(path, entry);

                throw;
            }

            return entry;
        }

        private async Task Load(string path, Entry entry)
        {
            var import = new GltfImport(new ContentDownloadProvider(_content),
                materialGenerator: new MaterialGenerator());
            var addon = new AnimationAddon();

            import.AddImportAddonInstance(addon);

            try
            {
                if (!await import.Load(ContentDownloadProvider.UriFor(path)))
                {
                    throw new InvalidOperationException($"{path} did not load");
                }
            }
            catch
            {
                import.Dispose();

                throw;
            }

            entry.Import = import;
            entry.Clips = addon.Processor?.Result ?? new List<HostedClip>();
            entry.Materials = new MaterialAnimations(import);
        }

        private void Vacate(string path, Entry entry)
        {
            if (_entries.TryGetValue(path, out var current) && ReferenceEquals(current, entry))
            {
                _entries.Remove(path);
            }
        }

        private static void Play(Instantiator instantiator, List<HostedClip> clips)
        {
            foreach (var hosted in clips)
            {
                var host = hosted.HostNode >= 0
                    ? instantiator.NodeObject((uint)hosted.HostNode)
                    : instantiator.Scene.gameObject;

                if (host == null)
                {
                    Log.Warning($"clip '{hosted.Clip.name}' has no node to play on");

                    continue;
                }

                var first = !host.TryGetComponent<Animation>(out var animation);

                if (first)
                {
                    animation = host.AddComponent<Animation>();
                    animation.playAutomatically = true;
                }

                animation.AddClip(hosted.Clip, hosted.Clip.name);

                if (first)
                {
                    animation.clip = hosted.Clip;
                    animation.Play();
                }
            }
        }
    }
}
