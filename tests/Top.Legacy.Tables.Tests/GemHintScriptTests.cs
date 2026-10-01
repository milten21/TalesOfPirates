using System.IO;
using NUnit.Framework;
using Top.Legacy.Tables.Custom;

namespace Top.Legacy.Tables.Tests
{
    public class GemHintScriptTests
    {
        private static GemHintScript Read()
        {
            using var stream = File.OpenRead(Fixtures.Path("lua/scripts.lua"));

            return GemHintScript.Read(stream);
        }

        [Test]
        public void A_hint_function_reads_as_its_text_and_the_factor_it_multiplies_the_level_by()
        {
            var hints = Read().Hints;

            Assert.That(hints["ItemHint_LieYanS"], Is.EqualTo(new GemHint("Gem Bonus Attack +", 4)));
            Assert.That(hints["ItemHint_LongZhiXin"], Is.EqualTo(new GemHint("Gem Bonus Max HP +", 500)));
        }

        [Test]
        public void A_hint_text_reads_as_the_script_spells_it()
        {
            Assert.That(Read().Hints["ItemHint_YingYanS"], Is.EqualTo(new GemHint("Added Accuracy", 5)));
        }

        [Test]
        public void A_function_that_does_not_scale_a_text_by_the_level_is_not_a_hint()
        {
            var hints = Read().Hints;

            Assert.That(hints.ContainsKey("ItemHint_ABOLUO"), Is.False);
            Assert.That(hints.ContainsKey("GetElfSkill"), Is.False);
        }
    }
}
