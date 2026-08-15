using System.Collections.Generic;
using Top.Contracts.Tables;

namespace Top.Conversion.Pipeline.Tables
{
    /// <summary>
    /// A table the pipeline emits. Entries are null when the client is
    /// missing the source table.
    /// </summary>
    public interface ITableUnit
    {
        string Name { get; }

        string Path { get; }

        IEnumerable<TableEntry> Entries();
    }
}
