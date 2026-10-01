using System;
using System.Linq;
using NUnit.Framework;
using Top.Client.Features.Inventory;
using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Tests.Features.Inventory;

public class ItemActionFailureTextTests
{
    [Test]
    public void Each_failure_the_server_can_report_has_its_own_text()
    {
        var texts = Enum.GetValues<ItemActionFailure>().Select(ItemActionFailureText.Describe).ToList();

        Assert.That(texts, Is.Unique);
        Assert.That(texts, Has.None.EqualTo(ItemActionFailureText.Describe((ItemActionFailure)99)));
    }

    [Test]
    public void A_failure_the_client_does_not_know_names_its_code()
    {
        Assert.That(ItemActionFailureText.Describe((ItemActionFailure)99), Does.Contain("99"));
    }
}
