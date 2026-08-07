using System.IO;
using Top.Assets.Conversion.Pipeline;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Where the original client sits and whether converting replaces what is
    /// already there. Persistence and the window's editing surface only: the
    /// pipeline gets a settings object built from this.
    /// </summary>
    [FilePath("UserSettings/TopImporter.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ImporterSettings : ScriptableSingleton<ImporterSettings>
    {
        [SerializeField] private string clientRoot = string.Empty;
        [SerializeField] private bool overwrite;

        public string ClientRoot
        {
            get
            {
                if (!string.IsNullOrEmpty(clientRoot))
                {
                    return clientRoot;
                }

                var fallback = Path.GetFullPath("../../reference/assets");
                return Directory.Exists(fallback) ? fallback : string.Empty;
            }
            set
            {
                clientRoot = value;
                Save(true);
            }
        }

        public bool Overwrite
        {
            get => overwrite;
            set
            {
                if (overwrite != value)
                {
                    overwrite = value;
                    Save(true);
                }
            }
        }

        internal string RawClientRoot => clientRoot;

        public bool IsValid => !string.IsNullOrEmpty(ClientRoot) && Directory.Exists(ClientRoot);

        /// <summary>
        /// What the pipeline runs on: this project's content folder as the
        /// output root, the client as the input.
        /// </summary>
        public ConversionSettings Conversion()
        {
            return new ConversionSettings(ClientRoot, Path.GetFullPath(ContentAssets.Root), Overwrite);
        }

        /// <summary>
        /// A pipeline over the configured client, or null when there is no
        /// client to run it on.
        /// </summary>
        public static ConversionPipeline OpenPipeline()
        {
            if (instance.IsValid)
            {
                return new ConversionPipeline(instance.Conversion());
            }

            Log.Error("original client root is not set or does not exist, configure it in Top/Importer");

            return null;
        }
    }
}
