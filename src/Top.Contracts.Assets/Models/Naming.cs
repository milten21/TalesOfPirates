using System;

namespace Top.Contracts.Assets.Models
{
    /// <summary>
    /// The names converted models carry: geometry and dummy nodes, helper and
    /// lit-shell meshes, materials, and animation clips.
    /// </summary>
    public static class Naming
    {
        private const string GeometryPrefix = "geom_";
        private const string DummyPrefix = "dummy_";
        private const string HelperPrefix = "helper_";
        private const string LitShellSuffix = "_lit";
        private const string SkeletonSuffix = "_skeleton";

        public static string Geometry(uint objectId)
        {
            return GeometryPrefix + objectId;
        }

        public static bool IsGeometry(string name)
        {
            return name.StartsWith(GeometryPrefix, StringComparison.Ordinal);
        }

        public static string LitShell(string geometryName)
        {
            return geometryName + LitShellSuffix;
        }

        public static bool IsLitShell(string name)
        {
            return name.EndsWith(LitShellSuffix, StringComparison.Ordinal);
        }

        public static string SkeletonContainer(string geometryName)
        {
            return geometryName + SkeletonSuffix;
        }

        public static string Dummy(uint dummyId)
        {
            return DummyPrefix + dummyId;
        }

        public static string Helper(string meshName, int index)
        {
            return string.IsNullOrEmpty(meshName)
                ? HelperPrefix + "mesh_" + index
                : HelperPrefix + meshName.ToLowerInvariant() + "_" + index;
        }

        public static bool IsHelper(string name)
        {
            return name.StartsWith(HelperPrefix, StringComparison.Ordinal);
        }

        public static string Material(string modelName, uint objectId, int subset)
        {
            return modelName + "_" + objectId + "_" + subset;
        }

        public static bool TryParseMaterial(string materialName, out uint objectId, out int subset)
        {
            objectId = 0;
            subset = 0;

            var lastSeparator = materialName?.LastIndexOf('_') ?? -1;

            if (lastSeparator <= 0)
            {
                return false;
            }

            var objectSeparator = materialName!.LastIndexOf('_', lastSeparator - 1);

            return objectSeparator > 0
                   && uint.TryParse(
                       materialName.Substring(objectSeparator + 1, lastSeparator - objectSeparator - 1),
                       out objectId)
                   && int.TryParse(materialName.Substring(lastSeparator + 1), out subset)
                   && subset >= 0;
        }

        public static string ActionClip(string modelName, int actionNo)
        {
            if (actionNo < 1)
            {
                return modelName + "_timeline";
            }

            return modelName + "_" + ActionClip(actionNo);
        }

        public static string ActionClip(int actionNo)
        {
            return ActionNames.TryGetName(actionNo, out var slug)
                ? $"{actionNo:00}_{slug}"
                : $"{actionNo:00}";
        }
    }
}
