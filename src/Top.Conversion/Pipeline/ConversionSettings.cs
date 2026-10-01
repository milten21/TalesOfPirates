namespace Top.Conversion.Pipeline
{
    /// <summary>
    /// Where the original client's files are, where converted output lands,
    /// and whether a run replaces what is already there.
    /// </summary>
    public class ConversionSettings
    {
        public ConversionSettings(string clientRoot, string outputRoot, bool overwrite = false,
            int mapChunkSize = Maps.MapBuilder.DefaultChunkSize)
        {
            ClientRoot = clientRoot;
            OutputRoot = outputRoot;
            Overwrite = overwrite;
            MapChunkSize = mapChunkSize;
            Source = new SourcePaths(clientRoot);
            Output = new OutputPaths(outputRoot);
        }

        public string ClientRoot { get; }

        public string OutputRoot { get; }

        public bool Overwrite { get; }

        public int MapChunkSize { get; }

        public SourcePaths Source { get; }

        public OutputPaths Output { get; }
    }
}
