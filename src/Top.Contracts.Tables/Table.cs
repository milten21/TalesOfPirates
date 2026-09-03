using System.Collections;
using System.Collections.Generic;

namespace Top.Contracts.Tables
{
    public class Table<TEntry> : IReadOnlyCollection<TEntry> where TEntry : TableEntry
    {
        private readonly List<TEntry> _entries;
        private readonly Dictionary<int, TEntry> _byId;

        protected Table(IEnumerable<TEntry> entries)
        {
            _entries = new List<TEntry>(entries);
            _byId = new Dictionary<int, TEntry>(_entries.Count);

            foreach (var entry in _entries)
            {
                _byId[entry.Id] = entry;
            }
        }

        public int Count => _entries.Count;

        public TEntry this[int id] => _byId[id];

        public bool TryGetById(int id, out TEntry entry)
        {
            return _byId.TryGetValue(id, out entry);
        }

        public IEnumerator<TEntry> GetEnumerator()
        {
            return _entries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
