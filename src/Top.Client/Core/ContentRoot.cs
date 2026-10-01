using System.IO;
using Top.Content;

namespace Top.Client.Core;

public class ContentRoot
{
    public ContentRoot(string folder)
    {
        Folder = Path.GetFullPath(folder);

        Source = new FolderContentSource(Folder);
    }

    public string Folder { get; }

    public IContentSource Source { get; }

    public override string ToString()
    {
        return Folder;
    }
}
