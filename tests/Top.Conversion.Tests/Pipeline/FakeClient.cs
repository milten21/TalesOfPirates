using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using Top.Conversion.Pipeline;
using Top.Legacy.Tables.Custom;
using Original = Top.Legacy.MindPower.World;

namespace Top.Conversion.Tests.Pipeline
{
    /// <summary>
    /// A throwaway client tree beside a throwaway output root. Original files
    /// go in by copying fixtures, converted ones come out where the settings
    /// say. Both are gone when the test ends.
    /// </summary>
    internal class FakeClient : IDisposable
    {
        private readonly string _root;

        internal FakeClient()
        {
            _root = Path.Combine(Path.GetTempPath(), "top-pipeline-tests",
                TestContext.CurrentContext.Test.ID);

            Delete();

            ClientRoot = Path.Combine(_root, "client");
            OutputRoot = Path.Combine(_root, "Content");

            Directory.CreateDirectory(ClientRoot);
            Directory.CreateDirectory(OutputRoot);
        }

        internal string ClientRoot { get; }

        internal string OutputRoot { get; }

        internal ConversionSettings Settings(bool overwrite = true)
        {
            return new ConversionSettings(ClientRoot, OutputRoot, overwrite);
        }

        /// <summary>
        /// Copies a fixture model into one of the client's model folders and
        /// returns where it landed.
        /// </summary>
        internal string AddModel(string kind, string fixture, string fileName = null)
        {
            var source = Fixtures.Path(fixture);
            var path = Path.Combine(ClientRoot, "model", kind,
                fileName ?? Path.GetFileName(source));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(source, path, overwrite: true);

            return path;
        }

        internal string AddSkeleton(string fixture, string fileName = null)
        {
            var source = Fixtures.Path(fixture);
            var path = Path.Combine(ClientRoot, "animation", fileName ?? Path.GetFileName(source));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(source, path, overwrite: true);

            return path;
        }

        internal void AddTexture(string kind, string fixture, string fileName)
        {
            var path = Path.Combine(ClientRoot, "texture", kind, fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.Copy(Fixtures.Path(fixture), path, overwrite: true);
        }

        internal void AddTextures(string kind, string fixtureDir)
        {
            var target = Path.Combine(ClientRoot, "texture", kind);

            Directory.CreateDirectory(target);

            foreach (var file in Directory.GetFiles(Fixtures.Path(fixtureDir)))
            {
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
            }
        }

        internal void AddWaterLoop()
        {
            for (var frame = 1; frame <= 30; frame++)
            {
                AddTexture("terrain/water", "bmp/1.BMP", $"ocean_h.{frame:00}.bmp");
            }
        }

        internal void AddMap(string name, Original.MapFile terrain, Original.ObjFile objects = null)
        {
            using (var stream = File.Create(MapPath(name, ".map")))
            {
                terrain.Write(stream);
            }

            if (objects == null)
            {
                return;
            }

            using var objectStream = File.Create(MapPath(name, ".obj"));

            objects.Write(objectStream);
        }

        internal void AddUnreadableMap(string name)
        {
            File.WriteAllBytes(MapPath(name, ".map"), new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });
        }

        internal string Converted(string kind, string name)
        {
            return Path.Combine(OutputRoot, "models", kind, name + ".glb");
        }

        internal string ConvertedMap(string name)
        {
            return Path.Combine(OutputRoot, "maps", name + ".map");
        }

        internal string ConvertedRig(string name)
        {
            return Path.Combine(OutputRoot, "rigs", name + ".glb");
        }

        internal string ConvertedTexture(string kind, string name)
        {
            return Path.Combine(OutputRoot, "textures", kind, name);
        }

        /// <summary>
        /// An action table holding one clip for one action set.
        /// </summary>
        internal static CharacterActionTable ActionSet(int id)
        {
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes($"{id}\n\t1 0 10 0 5\n"));

            return CharacterActionTable.Read(stream);
        }

        public void Dispose()
        {
            Delete();
        }

        private string MapPath(string name, string extension)
        {
            var directory = Path.Combine(ClientRoot, "map");

            Directory.CreateDirectory(directory);

            return Path.Combine(directory, name + extension);
        }

        private void Delete()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
    }
}
