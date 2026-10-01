using NUnit.Framework;
using Top.Contracts.Assets.Models;

namespace Top.Contracts.Assets.Tests
{
    /// <summary>
    /// Pins the agreement between each formatter and its matcher. The formats
    /// themselves are pinned against real models by the mapper tests, which
    /// spell them out as literals so a typo here cannot pass both sides.
    /// </summary>
    public class NamingTests
    {
        [Test]
        public void Helpers_match_the_names_they_are_given()
        {
            Assert.That(Naming.IsHelper(Naming.Helper("Block", 0)), Is.True);
            Assert.That(Naming.IsHelper(Naming.Helper(string.Empty, 3)), Is.True);
            Assert.That(Naming.IsHelper(Naming.Geometry(0)), Is.False);
        }

        [Test]
        public void Geometry_matches_the_names_it_is_given()
        {
            Assert.That(Naming.IsGeometry(Naming.Geometry(4)), Is.True);
            Assert.That(Naming.IsGeometry(Naming.Dummy(4)), Is.False);
            Assert.That(Naming.IsGeometry(Naming.Helper("Block", 0)), Is.False);
        }

        [Test]
        public void Lit_shells_match_the_names_they_are_given()
        {
            Assert.That(Naming.IsLitShell(Naming.LitShell(Naming.Geometry(7))), Is.True);
        }

        [Test]
        public void Skeleton_containers_are_not_lit_shells()
        {
            // Both hang off the same geom_<id> stem, so a suffix match is the
            // nearest thing in the layout to a false positive.
            Assert.That(
                Naming.IsLitShell(Naming.SkeletonContainer(Naming.Geometry(1))),
                Is.False);
        }

        [Test]
        public void Material_names_give_their_identity_back()
        {
            Assert.That(Naming.TryParseMaterial(Naming.Material("0001", 12, 3),
                out var objectId, out var subset), Is.True);
            Assert.That(objectId, Is.EqualTo(12u));
            Assert.That(subset, Is.EqualTo(3));
        }

        [Test]
        public void A_model_name_carrying_separators_still_parses()
        {
            Assert.That(Naming.TryParseMaterial(Naming.Material("hut_roof_2", 0, 1),
                out var objectId, out var subset), Is.True);
            Assert.That(objectId, Is.EqualTo(0u));
            Assert.That(subset, Is.EqualTo(1));
        }

        [Test]
        public void A_name_from_another_tool_does_not_parse()
        {
            Assert.That(Naming.TryParseMaterial("Material.001", out _, out _), Is.False);
            Assert.That(Naming.TryParseMaterial("wood_trim", out _, out _), Is.False);
            Assert.That(Naming.TryParseMaterial("wood_-1_0", out _, out _), Is.False);
            Assert.That(Naming.TryParseMaterial(null, out _, out _), Is.False);
        }
    }
}
