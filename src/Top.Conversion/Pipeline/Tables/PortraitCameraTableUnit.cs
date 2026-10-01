using System.Collections.Generic;
using System.Linq;
using Top.Contracts.Tables;
using Top.Contracts.Tables.Characters;
using Top.Legacy.Tables.Custom;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// Emits the portrait camera of each character type that CharacterConf.clu
    /// sets. Entry 0 is the camera of every type the script does not set. The
    /// script frames the head of a player type for a 36 px square; the player
    /// types frame the head and shoulders for the 74 px circle of the redesign.
    /// </summary>
    public class PortraitCameraTableUnit(ClientTables tables) : ITableUnit
    {
        private static readonly Dictionary<int, PortraitCameraEntry> HeadAndShouldersCameras =
            new PortraitCameraEntry[]
            {
                new PortraitCameraEntry { Id = 1, Angle = 30, Distance = 2.25f, Height = 0.65f },
                new PortraitCameraEntry { Id = 2, Angle = 30, Distance = 2.7f, Height = 0.55f },
                new PortraitCameraEntry { Id = 3, Angle = 30, Distance = 2.25f, Height = 0.45f },
                new PortraitCameraEntry { Id = 4, Angle = 30, Distance = 2.1f, Height = 0.65f },
            }.ToDictionary(entry => entry.Id);

        public string Name => "portraitcameras";

        public string Path => PortraitCameraTable.Path;

        public IEnumerable<TableEntry> Entries()
        {
            return tables.ClientAttributes?.Attributes
                .OrderBy(typeAttribute => typeAttribute.Key)
                .Select(typeAttribute => ToEntry(typeAttribute.Key, typeAttribute.Value));
        }

        private static PortraitCameraEntry ToEntry(int type, ClientAttribute attribute)
        {
            return HeadAndShouldersCameras.GetValueOrDefault(type) ?? new PortraitCameraEntry
            {
                Id = type,
                Angle = attribute.PortraitAngle,
                Distance = attribute.PortraitDistance,
                Height = attribute.PortraitHeight,
            };
        }
    }
}
