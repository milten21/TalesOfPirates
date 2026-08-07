using System;
using System.IO;
using System.Linq;
using Top.Assets.Conversion.Pipeline;
using Top.Logging;
using UnityEditor;
using UnityEngine;

namespace Top.Assets.Tooling
{
    /// <summary>
    /// Editor window in Unity that provides an interface for importing assets.
    /// This window can be accessed through the "Top/Importer" menu item within Unity's editor.
    /// </summary>
    public class ImporterWindow : EditorWindow
    {
        private int _characterModel;
        private int _itemId;
        private int _sceneObjectId;
        private bool _batchCharacters = true;
        private bool _batchItems = true;
        private bool _batchSceneObjects = true;

        [MenuItem("Top/Importer")]
        public static void Open()
        {
            GetWindow<ImporterWindow>("Top Importer");
        }

        private void OnGUI()
        {
            DrawSettings();
            EditorGUILayout.Space();
            DrawFileConversion();
            EditorGUILayout.Space();
            DrawTableConversion();
            EditorGUILayout.Space();
            DrawBatch();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);

            var settings = ImporterSettings.instance;

            using (new EditorGUILayout.HorizontalScope())
            {
                var root = EditorGUILayout.TextField("Client root", settings.ClientRoot);

                if (root != settings.ClientRoot)
                {
                    settings.ClientRoot = root;
                }

                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    var picked = EditorUtility.OpenFolderPanel(
                        "Original client assets root", settings.ClientRoot, string.Empty);

                    if (!string.IsNullOrEmpty(picked))
                    {
                        settings.ClientRoot = picked;
                        GUI.FocusControl(null);
                    }
                }
            }

            EditorGUILayout.LabelField(" ", Status(settings), EditorStyles.miniLabel);

            settings.Overwrite = EditorGUILayout.ToggleLeft("Overwrite existing", settings.Overwrite);
        }

        private static string Status(ImporterSettings settings)
        {
            if (!settings.IsValid)
            {
                return "client root not found";
            }

            var missing = new[] { "model", "animation", "texture", "scripts" }
                .Where(dir => !Directory.Exists(Path.Combine(settings.ClientRoot, dir)))
                .ToList();

            return missing.Count == 0
                ? "all expected subfolders found"
                : "missing: " + string.Join(", ", missing);
        }

        private void DrawFileConversion()
        {
            EditorGUILayout.LabelField("File conversion", EditorStyles.boldLabel);

            var client = ImporterSettings.instance.Conversion();

            using (new EditorGUILayout.HorizontalScope())
            {
                FilePicker("Model (.lmo/.lgo)...", "Select model",
                    client.Source.Models, new[] { "Model files", "lmo,lgo" }, ModelDriver.Convert);

                FilePicker("Rig (.lab)...", "Select skeleton",
                    client.Source.Animations, new[] { "Skeleton files", "lab" }, CharacterDriver.ConvertRig);

                FilePicker("Character (.lab)...", "Select skeleton",
                    client.Source.Animations, new[] { "Skeleton files", "lab" }, CharacterDriver.ConvertModel);
            }
        }

        private static void FilePicker(string label, string title, string directory,
            string[] filters, Action<string> convert)
        {
            if (!GUILayout.Button(label))
            {
                return;
            }

            var path = EditorUtility.OpenFilePanelWithFilters(title, directory, filters);

            if (!string.IsNullOrEmpty(path))
            {
                convert(path);
            }
        }

        private void DrawTableConversion()
        {
            EditorGUILayout.LabelField("Table conversion", EditorStyles.boldLabel);

            _characterModel = IdRow("Character model id", _characterModel, minimum: 0,
                convert: CharacterDriver.ConvertModel);
            _itemId = IdRow("Item id", _itemId, minimum: 1, convert: ItemDriver.Convert);
            _sceneObjectId = IdRow("Scene object id", _sceneObjectId, minimum: 1,
                convert: SceneObjectDriver.Convert);
        }

        private static int IdRow(string label, int value, int minimum, Action<int> convert)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                value = EditorGUILayout.IntField(label, value);

                using (new EditorGUI.DisabledScope(value < minimum))
                {
                    if (GUILayout.Button("Convert", GUILayout.Width(80)))
                    {
                        convert(value);
                    }
                }
            }

            return value;
        }

        private void DrawBatch()
        {
            EditorGUILayout.LabelField("Batch", EditorStyles.boldLabel);

            _batchCharacters = EditorGUILayout.ToggleLeft("Characters", _batchCharacters);
            _batchItems = EditorGUILayout.ToggleLeft("Items", _batchItems);
            _batchSceneObjects = EditorGUILayout.ToggleLeft("Scene objects", _batchSceneObjects);

            using (new EditorGUI.DisabledScope(!(_batchCharacters || _batchItems || _batchSceneObjects)))
            {
                if (GUILayout.Button("Convert client"))
                {
                    RunBatch();
                }
            }
        }

        private void RunBatch()
        {
            var pipeline = ImporterSettings.OpenPipeline();

            if (pipeline == null)
            {
                return;
            }

            using var progress = new EditorProgress();

            if (_batchCharacters && !progress.Canceled)
            {
                CharacterDriver.ConvertAll(pipeline, progress);
            }

            if (_batchItems && !progress.Canceled)
            {
                ItemDriver.ConvertAll(pipeline, progress);
            }

            if (_batchSceneObjects && !progress.Canceled)
            {
                SceneObjectDriver.ConvertAll(pipeline, progress);
            }

            if (progress.Canceled)
            {
                Log.Info("batch canceled");
            }
        }
    }
}
