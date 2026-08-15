using System;
using Top.Client.Core;
using UnityEngine;

namespace Top.Client.Assets.Models
{
    /// <summary>
    /// One live copy of a model. Disposing destroys its object tree and drops
    /// its claim on the import the whole model came from.
    /// </summary>
    public class ModelInstance : IDisposable
    {
        private readonly string _path;

        private ModelStore _store;

        internal ModelInstance(ModelStore store, string path, Transform root)
        {
            _store = store;
            _path = path;
            Root = root;
        }

        public Transform Root { get; private set; }

        public void Dispose()
        {
            if (_store == null)
            {
                return;
            }

            var store = _store;

            _store = null;

            if (Root != null)
            {
                UnityObjects.Destroy(Root.gameObject);
            }

            Root = null;

            store.Release(_path);
        }
    }
}
