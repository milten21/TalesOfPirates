namespace Top.Client.Messages.Play.Data
{
    public record Fairy(
        int ItemId,
        int? AppearanceItemId,
        int Level,
        int Stamina,
        int MaxStamina,
        int Growth,
        int MaxGrowth);
}
