using NUnit.Framework;
using Top.Client.UI;

namespace Top.Client.Tests.UI
{
    public class ThemePathsTests
    {
        [Test]
        public void A_name_becomes_a_path_beside_the_default()
        {
            Assert.That(ThemePaths.ToPath("Classic"), Is.EqualTo("res://UI/Assets/Classic.tres"));
        }

        [Test]
        public void The_default_name_becomes_the_default_theme_path()
        {
            Assert.That(ThemePaths.ToPath(ThemePaths.DefaultName), Is.EqualTo("res://UI/Assets/Default.tres"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void A_name_that_says_nothing_falls_back_to_the_default(string name)
        {
            Assert.That(ThemePaths.ToPath(name), Is.EqualTo("res://UI/Assets/Default.tres"));
        }

        [TestCase("../secrets")]
        [TestCase("nested/Classic")]
        [TestCase("nested\\Classic")]
        [TestCase("res://UI/Assets/Classic")]
        public void A_name_that_leaves_the_themes_folder_falls_back_to_the_default(string name)
        {
            Assert.That(ThemePaths.ToPath(name), Is.EqualTo("res://UI/Assets/Default.tres"));
        }

        [Test]
        public void A_name_loses_the_spaces_around_it()
        {
            Assert.That(ThemePaths.ToPath("  Classic  "), Is.EqualTo("res://UI/Assets/Classic.tres"));
        }
    }
}
