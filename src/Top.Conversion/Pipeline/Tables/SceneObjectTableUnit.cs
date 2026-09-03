using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.World;
using Top.Legacy.Tables.Records;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Maps sceneobjinfo rows to scene-object entries.
    /// </summary>
    public class SceneObjectTableUnit(ClientTables tables) : ITableUnit
    {
        public string Name => "sceneobjects";

        public string Path => SceneObjectTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.SceneObjects?.Select(Entry);
        }

        private static SceneObjectEntry Entry(SceneObjectInfoRecord row)
        {
            var kind = (SceneObjectKind)row.Type;

            SceneObjectEntry entry = kind switch
            {
                SceneObjectKind.Model when row.FadeObjSeq.Length > 0 => new FadeEntry
                {
                    Sequence = row.FadeObjSeq,
                    Coefficient = row.FadeCoefficient,
                },
                SceneObjectKind.PointLight => new PointLightEntry
                {
                    Color = TableColor.Read(row.PointColor),
                    Range = row.PointLightRange,
                    Attenuation = row.PointLightAttenuation,
                    AnimationId = row.PointLightAnimCtrlId,
                },
                SceneObjectKind.AmbientLight => new AmbientLightEntry { Color = TableColor.Read(row.EnvColor) },
                SceneObjectKind.Fog => new FogEntry { Color = TableColor.Read(row.FogColor) },
                SceneObjectKind.Sound => new SoundEntry { Sound = row.EnvSound, Distance = row.EnvSoundDistance },
                _ => new SceneObjectEntry(),
            };

            entry.Id = row.Id;
            entry.ModelPath = !string.IsNullOrEmpty(row.Name)
                ? OutputPaths.ModelContentPath(ContentKind.Scene, System.IO.Path.GetFileNameWithoutExtension(row.Name))
                : null;
            entry.DisplayName = string.IsNullOrEmpty(row.DisplayName) ? null : row.DisplayName;
            entry.Kind = kind;
            entry.AttachEffectId = row.AttachEffectId;
            entry.EnableEnvLight = row.EnableEnvLight;
            entry.EnablePointLight = row.EnablePointLight;
            entry.Style = row.Style;
            entry.Flag = row.Flag;
            entry.SizeFlag = row.SizeFlag;
            entry.ShadeFlag = row.ShadeFlag;
            entry.IsReallyBig = row.IsReallyBig;

            return entry;
        }
    }
}
