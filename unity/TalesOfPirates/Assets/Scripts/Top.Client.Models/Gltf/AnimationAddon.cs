using GLTFast;
using GLTFast.Addons;
using GLTFast.Animations;

namespace Top.Client.Models.Gltf
{
    /// <summary>
    /// Replaces glTFast's stock clip creation with legacy clips whose curve
    /// paths are relative to the common ancestor of the nodes each clip
    /// drives, so every clip can live on its own subtree instead of one
    /// Animation at the scene root.
    /// </summary>
    public class AnimationAddon : ImportAddonInstance, IAnimationProcessorFactory
    {
        public AnimationProcessor Processor { get; private set; }

        public IAnimationProcessor CreateAnimationProcessor(int clipCount)
        {
            Processor = new AnimationProcessor(clipCount);

            return Processor;
        }

        public override bool SupportsGltfExtension(string extensionName)
        {
            return false;
        }

        public override void Inject(GltfImportBase gltfImport)
        {
        }

        public override void Inject(IInstantiator instantiator)
        {
        }

        public override void Dispose()
        {
        }
    }
}
