namespace Top.Conversion.Models
{
    /// <summary>
    /// One distinct frame range of a timeline, played as a clip of its own.
    /// </summary>
    public record ClipRange(string Name, int StartFrame, int EndFrame);
}
