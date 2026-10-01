using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Top.Client.Core;

namespace Top.Client.EngineTests.Test;

public static class TestContent
{
    private const string ConversionCommand = "dotnet run --project src/Top.Conversion.Cli";

    private static readonly Lazy<ServerContent> LoadedContent = new Lazy<ServerContent>(Load);

    public static ServerContent ServerContent => LoadedContent.Value;

    private static ServerContent Load()
    {
        var artifactsPath = typeof(TestContent).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .First(attribute => attribute.Key == "ArtifactsPath").Value!;

        var folder = Path.Combine(artifactsPath, "content");

        if (!Directory.Exists(folder))
        {
            throw new DirectoryNotFoundException(
                $"No converted content at {folder}. Run '{ConversionCommand}' from the repo root first.");
        }

        return new ServerContent(folder);
    }
}
