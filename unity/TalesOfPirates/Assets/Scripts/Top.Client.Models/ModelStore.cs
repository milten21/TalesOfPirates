using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GLTFast;
using Top.Client.Models.Gltf;
using Top.Client.Models.Materials;
using Top.Client.Core;
using Top.Content;
using Top.Logging;
using UnityEngine;
using GltfImport = GLTFast.Newtonsoft.GltfImport;

namespace Top.Client.Models
{
    public class ModelStore : IModelStore
    {
        private class CachedModel
        {
            public Task LoadingTask;
            public GltfImport GltfImport;
            public List<HostedClip> HostedClips;
            public MaterialAnimationSet MaterialAnimations;
            public int InstanceCount;
        }

        private static readonly InstantiationSettings Settings = new InstantiationSettings
        {
            SceneObjectCreation = SceneObjectCreation.Always,
        };

        private readonly Dictionary<string, CachedModel> _cachedModels =
            new Dictionary<string, CachedModel>(StringComparer.Ordinal);

        private readonly IContentSource _contentSource;
        private readonly Shader _shader;

        public ModelStore(IContentSource contentSource, Shader shader)
        {
            _contentSource = contentSource ?? throw new ArgumentNullException(nameof(contentSource));
            _shader = shader;
        }

        public async Task<ModelInstance> Instantiate(string path, Transform parent,
            CancellationToken cancellationToken = default)
        {
            var cachedModel = await GetOrLoad(path);

            cancellationToken.ThrowIfCancellationRequested();

            var instantiator = new Instantiator(cachedModel.GltfImport, parent, cachedModel.MaterialAnimations,
                settings: Settings);
            var built = false;

            cachedModel.InstanceCount++;

            try
            {
                if (!await cachedModel.GltfImport.InstantiateMainSceneAsync(instantiator, cancellationToken))
                {
                    throw new InvalidOperationException($"{path} did not instantiate");
                }

                Play(instantiator, cachedModel.HostedClips);

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

        public Task EnsureLoaded(IEnumerable<string> paths)
        {
            var loads = paths.Select(GetOrLoad).Cast<Task>().ToList();

            return Task.WhenAll(loads);
        }

        internal void Release(string path)
        {
            if (!_cachedModels.TryGetValue(path, out var cachedModel))
            {
                return;
            }

            cachedModel.InstanceCount--;

            if (cachedModel.InstanceCount > 0)
            {
                return;
            }

            _cachedModels.Remove(path);
            cachedModel.MaterialAnimations.Dispose();
            cachedModel.GltfImport.Dispose();

            foreach (var hosted in cachedModel.HostedClips)
            {
                UnityObjects.Destroy(hosted.Clip);
            }
        }

        private async Task<CachedModel> GetOrLoad(string path)
        {
            if (!_cachedModels.TryGetValue(path, out var cachedModel))
            {
                cachedModel = new CachedModel();
                _cachedModels[path] = cachedModel;
                cachedModel.LoadingTask = Load(path, cachedModel);
            }

            try
            {
                await cachedModel.LoadingTask;
            }
            catch
            {
                Release(path, cachedModel);

                throw;
            }

            return cachedModel;
        }

        private async Task Load(string path, CachedModel cachedModel)
        {
            var import = new GltfImport(new ContentDownloadProvider(_contentSource),
                materialGenerator: new MaterialGenerator(_shader));
            var addon = new AnimationAddon();

            import.AddImportAddonInstance(addon);

            try
            {
                if (!await import.Load(ContentDownloadProvider.CreateUri(path)))
                {
                    throw new InvalidOperationException($"{path} did not load");
                }
            }
            catch
            {
                import.Dispose();

                throw;
            }

            cachedModel.GltfImport = import;
            cachedModel.HostedClips = addon.Processor?.Clips ?? new List<HostedClip>();
            cachedModel.MaterialAnimations = new MaterialAnimationSet(import);
        }

        private void Release(string path, CachedModel cachedModel)
        {
            if (_cachedModels.TryGetValue(path, out var cacheModel) && ReferenceEquals(cacheModel, cachedModel))
            {
                _cachedModels.Remove(path);
            }
        }

        private static void Play(Instantiator instantiator, List<HostedClip> clips)
        {
            foreach (var hostedClip in clips)
            {
                var host = hostedClip.HostNode >= 0
                    ? instantiator.FindNodeObject((uint)hostedClip.HostNode)
                    : instantiator.Scene.gameObject;

                if (host == null)
                {
                    Log.Warning($"clip '{hostedClip.Clip.name}' has no node to play on");

                    continue;
                }

                var isNewAnimation = !host.TryGetComponent<Animation>(out var animation);

                if (isNewAnimation)
                {
                    animation = host.AddComponent<Animation>();
                    animation.playAutomatically = true;
                }

                animation.AddClip(hostedClip.Clip, hostedClip.Clip.name);

                if (isNewAnimation)
                {
                    animation.clip = hostedClip.Clip;
                    animation.Play();
                }
            }
        }
    }
}
