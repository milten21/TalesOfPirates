using System.Threading;
using System.Threading.Tasks;

namespace Top.Client.Definitions
{
    /// <summary>
    /// Reads the converted tables into one immutable dictionary.
    /// </summary>
    public interface IDefinitionsStore
    {
        Task<DefinitionsDictionary> Load(CancellationToken cancel = default);
    }
}
