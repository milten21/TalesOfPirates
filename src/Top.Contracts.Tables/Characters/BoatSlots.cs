namespace Top.Contracts.Tables.Characters
{
    public static class BoatSlots
    {
        public const int Hull = 0;

        public const int Bow = 1;

        public const int Engine = 2;

        public const int Flag = 3;

        public const int FirstPropeller = 4;

        public const int PropellerCount = 4;

        public const int Count = FirstPropeller + PropellerCount;

        public const int Driving = Hull;

        public static int? ParentOf(int slot)
        {
            return slot == Hull ? null : Hull;
        }
    }
}
