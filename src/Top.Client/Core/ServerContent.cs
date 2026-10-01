using Top.Client.Rendering.Models;
using Top.Client.Rendering.Textures;

namespace Top.Client.Core;

public class ServerContent
{
    public ServerContent(string folder)
    {
        ContentRoot = new ContentRoot(folder);
        ModelFactory = new ModelFactory(ContentRoot.Folder);
        TableReader = new TableReader(ContentRoot.Source);
        TextureCache = new TextureCache(new TextureReader(ContentRoot.Source));
    }

    public ContentRoot ContentRoot { get; }

    public ModelFactory ModelFactory { get; }

    public TableReader TableReader { get; }

    public TextureCache TextureCache { get; }
}
