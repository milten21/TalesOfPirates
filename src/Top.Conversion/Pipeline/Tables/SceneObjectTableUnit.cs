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
            SceneObjectEntry entry = row.Type switch
            {
                0 when row.FadeObjSeq.Length > 0 => new FadeEntry
                {
                    Sequence = row.FadeObjSeq,
                    Coefficient = row.FadeCoefficient,
                },
                3 => new PointLightEntry
                {
                    Color = row.PointColor,
                    Range = row.PointLightRange,
                    Attenuation = row.PointLightAttenuation,
                    AnimationId = row.PointLightAnimCtrlId,
                },
                4 => new AmbientLightEntry { Color = row.EnvColor },
                5 => new FogEntry { Color = row.FogColor },
                6 => new SoundEntry { Sound = row.EnvSound, Distance = row.EnvSoundDistance },
                _ => new SceneObjectEntry(),
            };

            entry.Id = row.Id;
            entry.ModelPath = !string.IsNullOrEmpty(row.Name)
                ? OutputPaths.ModelContentPath(ContentKind.Scene, System.IO.Path.GetFileNameWithoutExtension(row.Name))
                : null;
            entry.DisplayName = string.IsNullOrEmpty(row.DisplayName) ? null : row.DisplayName;
            entry.Type = row.Type;
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
