using System;
using System.Text.Json;
using NUnit.Framework;
using Top.Client.Features.MainMenu.Servers;

namespace Top.Client.Tests.Features.MainMenu.Servers
{
    public class ServerEntryTests
    {
        private static readonly DateTime Monday = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime Tuesday = Monday.AddDays(1);

        [Test]
        public void A_name_a_host_and_a_port_parse()
        {
            Assert.That(ServerEntry.TryParse("Home", "127.0.0.1", "1973", out string name, out string host,
                out int port), Is.True);
            Assert.That(name, Is.EqualTo("Home"));
            Assert.That(host, Is.EqualTo("127.0.0.1"));
            Assert.That(port, Is.EqualTo(1973));
        }

        [Test]
        public void The_spaces_around_a_name_and_a_host_go_away()
        {
            Assert.That(ServerEntry.TryParse("  Home  ", "  127.0.0.1  ", " 1973 ", out string name,
                out string host, out _), Is.True);
            Assert.That(name, Is.EqualTo("Home"));
            Assert.That(host, Is.EqualTo("127.0.0.1"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void A_name_that_says_nothing_parses_nothing(string name)
        {
            Assert.That(ServerEntry.TryParse(name, "127.0.0.1", "1973", out string parsedName, out _, out _),
                Is.False);
            Assert.That(parsedName, Is.Null);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void A_host_that_says_nothing_parses_nothing(string host)
        {
            Assert.That(ServerEntry.TryParse("Home", host, "1973", out _, out string parsedHost, out _),
                Is.False);
            Assert.That(parsedHost, Is.Null);
        }

        [TestCase("0")]
        [TestCase("65536")]
        [TestCase("-1")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("1973a")]
        [TestCase("one")]
        public void A_port_outside_one_to_65535_parses_nothing(string port)
        {
            Assert.That(ServerEntry.TryParse("Home", "127.0.0.1", port, out string name, out _, out int parsedPort),
                Is.False);
            Assert.That(name, Is.Null);
            Assert.That(parsedPort, Is.Zero);
        }

        [TestCase("1")]
        [TestCase("65535")]
        public void The_ends_of_the_port_range_are_allowed(string port)
        {
            Assert.That(ServerEntry.TryParse("Home", "127.0.0.1", port, out _, out _, out _), Is.True);
        }

        [TestCase("top.example.com")]
        [TestCase("localhost")]
        [TestCase("::1")]
        [TestCase("192.168.1.10")]
        public void A_host_is_never_parsed(string host)
        {
            Assert.That(ServerEntry.TryParse("Home", host, "1973", out _, out string parsedHost, out _), Is.True);
            Assert.That(parsedHost, Is.EqualTo(host));
        }

        [Test]
        public void The_port_starts_at_the_one_the_original_connects_to()
        {
            Assert.That(ServerEntry.DefaultPort, Is.EqualTo(1973));
        }

        [Test]
        public void An_entry_reads_as_its_name_host_and_port()
        {
            Assert.That(CreateEntry().ToString(), Is.EqualTo("Home - 127.0.0.1:1973"));
        }

        [Test]
        public void A_new_entry_has_no_accounts_and_no_last_login()
        {
            ServerEntry entry = CreateEntry();

            Assert.That(entry.Accounts, Is.Empty);
            Assert.That(entry.LastAccount, Is.Null);
            Assert.That(entry.LastLogin, Is.Null);
        }

        [Test]
        public void An_account_is_held_one_time_however_often_it_logs_in()
        {
            ServerEntry entry = CreateEntry();

            entry.RememberLogin("Milten", Monday);
            entry.RememberLogin("Milten", Tuesday);

            Assert.That(entry.Accounts, Is.EquivalentTo(new[] { new AccountEntry("Milten", Tuesday) }));
        }

        [Test]
        public void The_last_account_is_the_one_with_the_most_recent_login()
        {
            ServerEntry entry = CreateEntry();

            entry.RememberLogin("Milten", Tuesday);
            entry.RememberLogin("Diego", Monday);

            Assert.That(entry.LastAccount?.Name, Is.EqualTo("Milten"));
            Assert.That(entry.LastLogin, Is.EqualTo(Tuesday));
        }

        [Test]
        public void An_account_with_no_login_time_is_never_the_last_account()
        {
            ServerEntry entry = CreateEntry();

            entry.Accounts = [new AccountEntry("Milten", null)];

            Assert.That(entry.LastAccount, Is.Null);
            Assert.That(entry.LastLogin, Is.Null);
        }

        [Test]
        public void A_forgotten_account_is_gone()
        {
            ServerEntry entry = CreateEntry();

            entry.RememberLogin("Milten", Tuesday);
            entry.RememberLogin("Diego", Monday);
            entry.ForgetAccount("Milten");

            Assert.That(entry.Accounts, Is.EquivalentTo(new[] { new AccountEntry("Diego", Monday) }));
            Assert.That(entry.LastAccount?.Name, Is.EqualTo("Diego"));
        }

        [Test]
        public void A_record_with_no_accounts_has_no_accounts()
        {
            ServerEntry entry = JsonSerializer.Deserialize<ServerEntry>(
                "{\"Name\":\"Home\",\"Host\":\"127.0.0.1\",\"Port\":1973}");

            Assert.That(entry.Accounts, Is.Empty);
            Assert.That(entry.LastAccount, Is.Null);
        }

        [Test]
        public void A_record_whose_account_list_reads_as_null_has_no_accounts()
        {
            ServerEntry entry = JsonSerializer.Deserialize<ServerEntry>(
                "{\"Name\":\"Home\",\"Host\":\"127.0.0.1\",\"Port\":1973,\"Accounts\":null}");

            Assert.That(entry.Accounts, Is.Not.Null.And.Empty);
        }

        [Test]
        public void A_record_that_repeats_an_account_holds_it_one_time()
        {
            ServerEntry entry = JsonSerializer.Deserialize<ServerEntry>(
                "{\"Name\":\"Home\",\"Host\":\"127.0.0.1\",\"Port\":1973,\"Accounts\":["
                + "{\"Name\":\"Milten\",\"LastLogin\":null},{\"Name\":\"Milten\",\"LastLogin\":null}]}");

            Assert.That(entry.Accounts, Has.Count.EqualTo(1));
        }

        [Test]
        public void An_entry_writes_its_accounts_and_their_times_and_reads_them_back()
        {
            ServerEntry entry = CreateEntry();

            entry.RememberLogin("Milten", Monday);

            ServerEntry read = JsonSerializer.Deserialize<ServerEntry>(JsonSerializer.Serialize(entry));

            Assert.That(read.Accounts, Is.EquivalentTo(new[] { new AccountEntry("Milten", Monday) }));
        }

        private static ServerEntry CreateEntry()
        {
            return new ServerEntry { Name = "Home", Host = "127.0.0.1", Port = 1973 };
        }
    }
}
