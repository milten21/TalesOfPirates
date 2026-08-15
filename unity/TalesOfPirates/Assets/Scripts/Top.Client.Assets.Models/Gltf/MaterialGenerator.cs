using GLTFast;
using GLTFast.Logging;
using GLTFast.Materials;
using GLTFast.Schema;
using Top.Client.Assets.Models.Materials;
using Top.Contracts.Assets.Models.Materials;
using UnityEngine;
using Material = UnityEngine.Material;

namespace Top.Client.Assets.Models.Gltf
{
    /// <summary>
    /// Builds a Top/Model palette material for every glTF material glTFast
    /// hands over, the render state resolved by
    /// <see cref="MaterialStateReader"/>.
    /// </summary>
    public class MaterialGenerator : IMaterialGenerator
    {
        public Material GetDefaultMaterial(bool pointsSupport = false)
        {
            return RenderStateMapper.CreateMaterial(new RenderState(), null);
        }

        public Material GenerateMaterial(MaterialBase gltfMaterial, IGltfReadable gltf, bool pointsSupport = false)
        {
            Texture2D texture = null;
            var textureInfo = gltfMaterial.PbrMetallicRoughness?.BaseColorTexture;

            if (textureInfo != null && textureInfo.index >= 0)
            {
                texture = gltf.GetTexture(textureInfo.index);
            }

            var material = RenderStateMapper.CreateMaterial(MaterialStateReader.Read(gltfMaterial), texture);

            material.name = gltfMaterial.name;

            return material;
        }

        public void SetLogger(ICodeLogger logger)
        {
        }
    }
}
