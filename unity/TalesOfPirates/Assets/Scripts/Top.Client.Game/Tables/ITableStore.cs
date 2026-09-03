using System.Threading;
using System.Threading.Tasks;

namespace Top.Client.Game.Tables
{
    public interface ITableStore
    {
        Task<TableSet> Load(CancellationToken cancel = default);
    }
}
