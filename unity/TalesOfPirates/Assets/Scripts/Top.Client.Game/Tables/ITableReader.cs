using System.Threading;
using System.Threading.Tasks;

namespace Top.Client.Game.Tables
{
    public interface ITableReader
    {
        Task<TableSet> Read(CancellationToken cancellationToken = default);
    }
}
