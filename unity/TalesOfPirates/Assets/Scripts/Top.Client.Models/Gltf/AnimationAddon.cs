using GLTFast;
using GLTFast.Addons;
using GLTFast.Animations;

namespace Top.Client.Models.Gltf
{
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
