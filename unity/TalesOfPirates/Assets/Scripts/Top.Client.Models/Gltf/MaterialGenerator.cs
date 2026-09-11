using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using Top.Client.Models.Materials;
using Top.Contracts.Assets.Models.Materials;
using UnityEngine;
using Material = UnityEngine.Material;

namespace Top.Client.Models.Gltf
{
    public class MaterialGenerator : IMaterialGenerator
    {
        private readonly Shader _shader;

        public MaterialGenerator(Shader shader)
        {
            _shader = shader;
        }

        public Material GetDefaultMaterial(bool pointsSupport = false)
        {
            return RenderStateMapper.CreateMaterial(new RenderState(), null, _shader);
        }

        public Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
        {
            Texture2D texture = null;
            var textureInfo = gltfMaterial.PbrMetallicRoughness?.BaseColorTexture;

            if (textureInfo != null && textureInfo.index >= 0)
            {
                texture = gltf.GetTexture(textureInfo.index);
            }

            var material = RenderStateMapper.CreateMaterial(MaterialStateReader.Read(gltfMaterial), texture, _shader);

            material.name = gltfMaterial.name;

            return material;
        }

        public void SetLogger(ICodeLogger logger)
        {
        }
    }
}
