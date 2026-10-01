using System;
using Top.Client.Messages.Play.Data;

namespace Top.Client.Messages.Account.Data
{
    public class CharacterSlot
    {
        public CharacterSlot(string name, string job, int level, int typeId, CharacterLook look)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Job = job ?? throw new ArgumentNullException(nameof(job));
            Level = level;
            TypeId = typeId;
            Look = look ?? throw new ArgumentNullException(nameof(look));
        }

        public string Name { get; }

        public string Job { get; }

        public int Level { get; }

        public int TypeId { get; }

        public CharacterLook Look { get; }
    }
}
