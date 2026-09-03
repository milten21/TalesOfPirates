using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Top.Content
{
    public class MemoryContentSource : IContentSource
    {
        private readonly Dictionary<string, byte[]> _files =
            new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        public void Add(string path, byte[] content)
        {
            ContentPath.CheckPath(path);

            _files[path.ToLowerInvariant()] = content;
        }

        public Task<byte[]> Read(string path)
        {
            ContentPath.CheckPath(path);

            return _files.TryGetValue(path, out var content)
                ? Task.FromResult((byte[])content.Clone())
                : Task.FromException<byte[]>(new FileNotFoundException("No content at " + path, path));
        }

        public bool Exists(string path)
        {
            ContentPath.CheckPath(path);

            return _files.ContainsKey(path);
        }

        public IEnumerable<string> List(string prefix)
        {
            return ContentPath.ListUnder(_files.Keys, prefix);
        }
    }
}
