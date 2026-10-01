using System.Globalization;
using Top.Client.Messages.Inventory.Data;

namespace Top.Client.Features.Inventory;

public static class ItemActionFailureText
{
    public static string Describe(ItemActionFailure failure)
    {
        return failure switch
        {
            ItemActionFailure.ItemMissing => "Item not found",
            ItemActionFailure.BagFull => "Your bag is full",
            ItemActionFailure.CannotUse => "Can't use this item",
            ItemActionFailure.CannotPickUp => "Can't pick up this item",
            ItemActionFailure.CannotThrow => "Can't throw this item",
            ItemActionFailure.CannotDelete => "Can't delete this item",
            ItemActionFailure.BagLocked => "Your bag is locked. Items can't be moved.",
            ItemActionFailure.TooFar => "Too far away",
            ItemActionFailure.LevelTooLow => "Your level is too low",
            ItemActionFailure.JobMismatched => "Your class can't use this item",
            ItemActionFailure.WrongState => "Can't use items right now",
            ItemActionFailure.Protected => "This item is protected",
            ItemActionFailure.WrongArea => "Not allowed in this area",
            ItemActionFailure.WrongBody => "Your character can't use this item",
            ItemActionFailure.WrongType => "Can't store this item",
            ItemActionFailure.Invalid => "This item is not valid",
            ItemActionFailure.OutOfBagRange => "That slot is not in your bag",
            ItemActionFailure.Expired => "This item has expired",
            _ => string.Create(CultureInfo.InvariantCulture, $"The item action failed. Error {(int)failure}"),
        };
    }
}
