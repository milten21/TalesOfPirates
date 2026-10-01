namespace Top.Client.Messages.Inventory.Data
{
    public enum ItemActionFailure
    {
        ItemMissing = 1,
        BagFull = 2,
        CannotUse = 3,
        CannotPickUp = 4,
        CannotThrow = 5,
        CannotDelete = 6,
        BagLocked = 7,
        TooFar = 8,
        LevelTooLow = 9,
        JobMismatched = 10,
        WrongState = 11,
        Protected = 12,
        WrongArea = 13,
        WrongBody = 14,
        WrongType = 15,
        Invalid = 16,
        OutOfBagRange = 17,
        Expired = 18,
    }
}
