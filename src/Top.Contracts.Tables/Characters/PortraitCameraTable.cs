using System.Collections.Generic;

namespace Top.Contracts.Tables.Characters
{
    public class PortraitCameraTable : Table<PortraitCameraEntry>
    {
        public const string Path = "tables/portraitcameras.json";

        private const int FallbackCharacterType = 0;

        public PortraitCameraTable(IEnumerable<PortraitCameraEntry> entries) : base(entries)
        {
        }

        public PortraitCameraEntry FindCamera(int characterType)
        {
            return TryGetById(characterType, out var entry) || TryGetById(FallbackCharacterType, out entry)
                ? entry
                : null;
        }
    }
}
