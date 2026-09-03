using System.Collections.Generic;
using System.Threading.Tasks;

namespace Top.Content
{
    public interface IContentSource
    {
        Task<byte[]> Read(string path);

        bool Exists(string path);

        IEnumerable<string> List(string prefix);
    }
}
