using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Top.Client.Features.MainMenu.Servers;

namespace Top.Client.Tests.Features.MainMenu.Servers
{
    public class ServerStoreTests
    {
        private const string FileWithAccountNames = """
            [
              {
                "Name": "Home",
                "Host": "127.0.0.1",
                "Port": 1973,
                "AccountNames": [ "Second", "First" ]
              },
              {
                "Name": "Live",
                "Host": "top.example.com",
                "Port": 2000,
                "AccountNames": [ "Only" ]
              },
              {
                "Name": "Empty",
                "Host": "top.example.com",
                "Port": 2001,
                "AccountNames": []
              }
            ]
            """;

        private const string FileWithAccountNamesAndLastAccountName = """
            [
              {
                "Name": "Home",
                "Host": "127.0.0.1",
                "Port": 1973,
                "AccountNames": [ "Second", "First" ],
                "LastAccountName": "First"
              },
              {
                "Name": "Live",
                "Host": "top.example.com",
                "Port": 2000,
                "AccountNames": [ "Only" ],
                "LastAccountName": "Only"
              }
            ]
            """;

        private static readonly DateTime Monday = new DateTime(2026, 9, 21, 10, 0, 0, DateTimeKind.Utc);

        private static readonly DateTime Tuesday = Monday.AddDays(1);

        private static readonly DateTime Wednesday = Monday.AddDays(2);

        private string _path;

        [SetUp]
        public void CreateTempPath()
        {
            _path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        [TearDown]
        public void RemoveTempFile()
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }

        [Test]
        public void A_store_over_a_file_that_is_not_there_holds_no_servers()
        {
            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries, Is.Empty);
            Assert.That(read.LastServer, Is.Null);
        }

        [Test]
        public void A_store_over_a_file_that_does_not_parse_holds_no_servers()
        {
            File.WriteAllText(_path, "{ this is not json");

            Assert.That(new ServerStore(_path).Entries, Is.Empty);
        }

        [Test]
        public void An_added_server_is_there_the_next_time_the_file_is_read()
        {
            new ServerStore(_path).Add("Home", "127.0.0.1", 1973);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries, Has.Count.EqualTo(1));
            Assert.That(read.Entries[0].Name, Is.EqualTo("Home"));
            Assert.That(read.Entries[0].Host, Is.EqualTo("127.0.0.1"));
            Assert.That(read.Entries[0].Port, Is.EqualTo(1973));
        }

        [Test]
        public void An_added_server_has_no_accounts_and_is_not_the_last_server()
        {
            ServerStore store = new ServerStore(_path);

            ServerEntry added = store.Add("Home", "127.0.0.1", 1973);

            Assert.That(added.Accounts, Is.Empty);
            Assert.That(added.LastAccount, Is.Null);
            Assert.That(store.LastServer, Is.Null);
        }

        [Test]
        public void Each_login_time_is_there_the_next_time_the_file_is_read()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Second", Monday);
            store.RememberLogin(store.Entries[0], "First", Tuesday);

            Assert.That(new ServerStore(_path).Entries[0].Accounts, Is.EquivalentTo(new[]
            {
                new AccountEntry("Second", Monday),
                new AccountEntry("First", Tuesday)
            }));
        }

        [Test]
        public void A_login_time_reads_back_as_utc()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Milten", Monday.ToLocalTime());

            DateTime? read = new ServerStore(_path).Entries[0].Accounts[0].LastLogin;

            Assert.That(read, Is.EqualTo(Monday));
            Assert.That(read?.Kind, Is.EqualTo(DateTimeKind.Utc));
        }

        [Test]
        public void An_account_that_logs_in_again_leaves_no_second_copy_and_takes_the_new_time()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Milten", Monday);
            store.RememberLogin(store.Entries[0], "Second", Tuesday);
            store.RememberLogin(store.Entries[0], "Milten", Wednesday);

            Assert.That(new ServerStore(_path).Entries[0].Accounts, Is.EquivalentTo(new[]
            {
                new AccountEntry("Milten", Wednesday),
                new AccountEntry("Second", Tuesday)
            }));
        }

        [Test]
        public void The_last_server_and_account_are_the_most_recent_login()
        {
            ServerStore store = new ServerStore(_path);

            ServerEntry home = store.Add("Home", "127.0.0.1", 1973);
            ServerEntry live = store.Add("Live", "top.example.com", 2000);

            store.RememberLogin(home, "Milten", Monday);
            store.RememberLogin(live, "Diego", Tuesday);
            store.RememberLogin(live, "Gorn", Wednesday);
            store.RememberLogin(live, "Diego", Monday);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.LastServer?.Name, Is.EqualTo("Live"));
            Assert.That(read.LastServer?.LastAccount?.Name, Is.EqualTo("Gorn"));
        }

        [Test]
        public void A_login_on_another_server_makes_it_the_last_server()
        {
            ServerStore store = new ServerStore(_path);

            ServerEntry home = store.Add("Home", "127.0.0.1", 1973);
            ServerEntry live = store.Add("Live", "top.example.com", 2000);

            store.RememberLogin(live, "Diego", Monday);
            store.RememberLogin(home, "Milten", Tuesday);

            Assert.That(new ServerStore(_path).LastServer?.Name, Is.EqualTo("Home"));
        }

        [Test]
        public void A_forgotten_account_and_its_time_are_gone_the_next_time_the_file_is_read()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Second", Monday);
            store.RememberLogin(store.Entries[0], "First", Tuesday);
            store.ForgetAccount(store.Entries[0], "First");

            ServerEntry read = new ServerStore(_path).Entries[0];

            Assert.That(read.Accounts, Is.EquivalentTo(new[] { new AccountEntry("Second", Monday) }));
            Assert.That(read.LastAccount?.Name, Is.EqualTo("Second"));
        }

        [Test]
        public void Forgetting_the_last_account_can_move_the_last_server()
        {
            ServerStore store = new ServerStore(_path);

            ServerEntry home = store.Add("Home", "127.0.0.1", 1973);
            ServerEntry live = store.Add("Live", "top.example.com", 2000);

            store.RememberLogin(home, "Milten", Monday);
            store.RememberLogin(live, "Diego", Tuesday);
            store.ForgetAccount(live, "Diego");

            Assert.That(new ServerStore(_path).LastServer?.Name, Is.EqualTo("Home"));
        }

        [Test]
        public void Forgetting_every_account_leaves_no_last_server()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Milten", Monday);
            store.ForgetAccount(store.Entries[0], "Milten");

            Assert.That(new ServerStore(_path).LastServer, Is.Null);
        }

        [Test]
        public void An_edited_server_reads_with_its_new_name_host_and_port()
        {
            ServerStore store = new ServerStore(_path);

            store.Add("Home", "127.0.0.1", 1973);
            store.Add("Second", "127.0.0.1", 1973);

            store.Edit(store.Entries[0], "Fixed", "top.example.com", 2000);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries, Has.Count.EqualTo(2));
            Assert.That(read.Entries[0].Name, Is.EqualTo("Fixed"));
            Assert.That(read.Entries[0].Host, Is.EqualTo("top.example.com"));
            Assert.That(read.Entries[0].Port, Is.EqualTo(2000));
            Assert.That(read.Entries[1].Name, Is.EqualTo("Second"));
        }

        [Test]
        public void Editing_a_server_keeps_its_accounts_and_their_times()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Milten", Monday);

            store.Edit(store.Entries[0], "Fixed", "top.example.com", 2000);

            Assert.That(new ServerStore(_path).Entries[0].Accounts,
                Is.EquivalentTo(new[] { new AccountEntry("Milten", Monday) }));
        }

        [Test]
        public void A_removed_server_takes_its_accounts_with_it()
        {
            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Add("Home", "127.0.0.1", 1973), "Milten", Monday);
            store.Remove(store.Entries[0]);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries, Is.Empty);
            Assert.That(read.LastServer, Is.Null);
        }

        [Test]
        public void Removing_the_last_server_makes_the_next_most_recent_server_the_last_server()
        {
            ServerStore store = new ServerStore(_path);

            ServerEntry home = store.Add("Home", "127.0.0.1", 1973);
            ServerEntry live = store.Add("Live", "top.example.com", 2000);
            ServerEntry test = store.Add("Test", "10.0.0.12", 1973);

            store.RememberLogin(home, "Milten", Tuesday);
            store.RememberLogin(test, "Lester", Monday);
            store.RememberLogin(live, "Diego", Wednesday);
            store.Remove(live);

            Assert.That(new ServerStore(_path).LastServer?.Name, Is.EqualTo("Home"));
        }

        [TestCase(FileWithAccountNames)]
        [TestCase(FileWithAccountNamesAndLastAccountName)]
        public void A_file_with_account_names_keeps_its_servers_and_accounts_with_no_times(string file)
        {
            File.WriteAllText(_path, file);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries[0].Name, Is.EqualTo("Home"));
            Assert.That(read.Entries[0].Accounts.Select(account => account.Name),
                Is.EquivalentTo(new[] { "First", "Second" }));
            Assert.That(read.Entries[1].Name, Is.EqualTo("Live"));
            Assert.That(read.Entries[1].Accounts.Select(account => account.Name),
                Is.EquivalentTo(new[] { "Only" }));
            Assert.That(read.Entries.SelectMany(entry => entry.Accounts), Has.All.Property("LastLogin").Null);
        }

        [TestCase(FileWithAccountNames)]
        [TestCase(FileWithAccountNamesAndLastAccountName)]
        public void A_file_with_account_names_has_no_last_server(string file)
        {
            File.WriteAllText(_path, file);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.LastServer, Is.Null);
            Assert.That(read.Entries.Select(entry => entry.LastAccount), Has.All.Null);
        }

        [Test]
        public void A_file_with_account_names_takes_login_times_from_then_on()
        {
            File.WriteAllText(_path, FileWithAccountNames);

            ServerStore store = new ServerStore(_path);

            store.RememberLogin(store.Entries[1], "Only", Monday);

            ServerStore read = new ServerStore(_path);

            Assert.That(read.Entries[0].Accounts, Has.Count.EqualTo(2));
            Assert.That(read.LastServer?.Name, Is.EqualTo("Live"));
            Assert.That(read.LastServer?.LastAccount, Is.EqualTo(new AccountEntry("Only", Monday)));
        }
    }
}
