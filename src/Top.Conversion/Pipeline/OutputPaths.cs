using System.IO;
using Top.Conversion.Textures;

namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// The converted tree. One glTF file per model under its kind, rigs of
    /// their own, textures shared by every model of a kind. Every segment
    /// below the root is written lowercase.
    /// </summary>
    public class OutputPaths(string root)
    {
        public string At(string contentPath) =>
            Path.Combine(root, contentPath.Replace('/', Path.DirectorySeparatorChar));

        public string Model(string kind, string name) => At(ModelContentPath(kind, name));

        public static string ModelContentPath(string kind, string name) => $"models/{Lower(kind)}/{Lower(name)}.glb";

        public string Rig(string name) => Path.Combine(root, "rigs", Lower(name) + ".glb");

        public string Map(string name) => At(MapContentPath(name));

        public static string MapContentPath(string name) => $"maps/{Lower(name)}.map";

        public string TextureDir(string kind) => Path.Combine(root, "textures", Lower(kind));

        public static string TextureContentPath(string kind, string fileName) =>
            $"textures/{Lower(kind)}/{TextureConversion.PngName(fileName)}";

        private static string Lower(string segment) => segment.ToLowerInvariant();
    }
}
