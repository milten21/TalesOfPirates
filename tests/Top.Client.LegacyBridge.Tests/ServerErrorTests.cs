using NUnit.Framework;

namespace Top.Client.LegacyBridge.Tests
{
    public class ServerErrorTests
    {
        [TestCase(1001, "Invalid Account")]
        [TestCase(1002, "Password incorrect")]
        [TestCase(1006, "Account has been banned")]
        [TestCase(1104, "Your account is already online")]
        [TestCase(7, "Client version error, server refused connection!")]
        public void A_code_the_original_names_gets_the_original_message(int code, string message)
        {
            Assert.That(ServerError.ToText(code), Is.EqualTo(message));
        }

        [TestCase(530, "Disband your guild before you delete its leader")]
        [TestCase(532, "The server is busy. Try again later")]
        [TestCase(538, "This character is in the game on another session")]
        public void A_code_the_redesign_rewords_gets_the_new_message(int code, string message)
        {
            Assert.That(ServerError.ToText(code), Is.EqualTo(message));
        }

        [TestCase(0)]
        [TestCase(1007)]
        [TestCase(1400)]
        public void A_code_the_original_leaves_out_gets_no_message(int code)
        {
            Assert.That(ServerError.ToText(code), Is.Empty);
        }
    }
}
