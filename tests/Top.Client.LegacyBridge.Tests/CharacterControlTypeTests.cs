using System;
using System.Linq;
using NUnit.Framework;
using Top.Client.Messages.Play.Data;

namespace Top.Client.LegacyBridge.Tests
{
    public class CharacterControlTypeTests
    {
        [Test]
        public void The_published_control_type_carries_the_values_the_character_table_declares()
        {
            var published = Enum.GetValues(typeof(CharacterControlType)).Cast<CharacterControlType>()
                .Select(value => (value.ToString(), (int)value));
            var declared = Enum.GetValues(typeof(Legacy.Tables.Records.CharacterControlType))
                .Cast<Legacy.Tables.Records.CharacterControlType>()
                .Select(value => (value.ToString(), (int)value));

            Assert.That(published, Is.EqualTo(declared), "the messages package cannot reference the tables package");
        }
    }
}
