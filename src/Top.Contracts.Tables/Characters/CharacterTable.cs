using System.Collections.Generic;

namespace Top.Contracts.Tables.Characters
{
    public class CharacterTable : Table<CharacterEntry>
    {
        public const string Path = "tables/characters.json";

        public CharacterTable(IEnumerable<CharacterEntry> entries) : base(entries)
        {
        }
    }
}
