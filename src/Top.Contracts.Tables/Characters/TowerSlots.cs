namespace Top.Contracts.Tables.Characters
{
    public static class TowerSlots
    {
        public const int Base = 0;

        public const int Head = 1;

        public const int Tube = 2;

        public const int Count = 3;

        public const int Driving = Head;

        public static int? ParentOf(int slot)
        {
            return slot switch
            {
                Head => Base,
                Tube => Head,
                _ => null
            };
        }
    }
}
