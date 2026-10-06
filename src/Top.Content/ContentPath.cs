using System;
using System.Collections.Generic;
using System.Linq;

namespace Top.Content
{
    internal static class ContentPath
    {
        private static readonly char[] ForbiddenCharacters = { ':', '*', '?', '"', '<', '>', '|' };

        public static void CheckPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("A content path cannot be empty.", nameof(path));
            }

            CheckSegments(path, nameof(path));
        }

        public static List<string> ListUnder(IEnumerable<string> paths, string prefix)
        {
            CheckPrefix(prefix);

            var folder = TrimTrailingSlash(prefix);
            var wanted = folder + "/";

            var matches = paths
                .Where(path => folder.Length == 0 || path.StartsWith(wanted, StringComparison.OrdinalIgnoreCase))
                .ToList();

            matches.Sort(StringComparer.Ordinal);

            return matches;
        }

        private static void CheckPrefix(string prefix)
        {
            if (prefix == null)
            {
                throw new ArgumentException("A content prefix cannot be null.", nameof(prefix));
            }

            if (prefix.Length == 0)
            {
                return;
            }

            CheckSegments(TrimTrailingSlash(prefix), nameof(prefix));
        }

        private static string TrimTrailingSlash(string prefix)
        {
            return prefix.EndsWith("/", StringComparison.Ordinal)
                ? prefix.Substring(0, prefix.Length - 1)
                : prefix;
        }

        private static void CheckSegments(string path, string argument)
        {
            if (path.IndexOf('\\') >= 0)
            {
                throw new ArgumentException("Content paths use forward slashes: " + path, argument);
            }

            if (path.Split('/').Any(IsUnsafeSegment))
            {
                throw new ArgumentException(
                    "Content paths carry one plain name per segment, relative to the root: " + path,
                    argument);
            }
        }

        private static bool IsUnsafeSegment(string segment)
        {
            return segment.Length == 0
                || segment == "."
                || segment == ".."
                || segment.EndsWith(".", StringComparison.Ordinal)
                || segment.EndsWith(" ", StringComparison.Ordinal)
                || segment.IndexOfAny(ForbiddenCharacters) >= 0
                || segment.Any(char.IsControl);
        }
    }
}
