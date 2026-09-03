using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Top.Content
{
    public class FolderContentSource : IContentSource
    {
        private readonly string _root;

        public FolderContentSource(string root)
        {
            _root = root;
        }

        public Task<byte[]> Read(string path)
        {
            var file = Resolve(path);

            return file != null
                ? File.ReadAllBytesAsync(file)
                : Task.FromException<byte[]>(new FileNotFoundException("No content at " + path, path));
        }

        public bool Exists(string path)
        {
            return Resolve(path) != null;
        }

        public IEnumerable<string> List(string prefix)
        {
            return ContentPath.ListUnder(ContentPaths(), prefix);
        }

        private IEnumerable<string> ContentPaths()
        {
            if (!Directory.Exists(_root))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                var path = Path.GetRelativePath(_root, file).Replace(Path.DirectorySeparatorChar, '/');

                yield return path.ToLowerInvariant();
            }
        }

        private string Resolve(string path)
        {
            ContentPath.CheckPath(path);

            var segments = path.Split('/');
            var directory = _root;

            for (var i = 0; i < segments.Length - 1; i++)
            {
                directory = ResolveDirectory(directory, segments[i]);

                if (directory == null)
                {
                    return null;
                }
            }

            return ResolveFile(directory, segments[segments.Length - 1]);
        }

        private static string ResolveDirectory(string parent, string name)
        {
            var asWritten = Path.Combine(parent, name);

            if (Directory.Exists(asWritten))
            {
                return asWritten;
            }

            return Directory.Exists(parent)
                ? MatchIgnoringCase(Directory.EnumerateDirectories(parent), name)
                : null;
        }

        private static string ResolveFile(string parent, string name)
        {
            var asWritten = Path.Combine(parent, name);

            if (File.Exists(asWritten))
            {
                return asWritten;
            }

            return Directory.Exists(parent)
                ? MatchIgnoringCase(Directory.EnumerateFiles(parent), name)
                : null;
        }

        private static string MatchIgnoringCase(IEnumerable<string> entries, string name)
        {
            foreach (var entry in entries)
            {
                if (string.Equals(Path.GetFileName(entry), name, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
