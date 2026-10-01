using Top.Client.Messages.Inventory.Data;

namespace Top.Client.LegacyBridge.Inventory
{
    public static class ItemActionFailureMapper
    {
        private const short KitbagFullResult = -4;

        public static ItemActionFailure ToFailure(short result)
        {
            return result == KitbagFullResult ? ItemActionFailure.BagFull : (ItemActionFailure)result;
        }
    }
}
