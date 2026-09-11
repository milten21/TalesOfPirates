using System;
using Top.Client.Core;
using UnityEngine;

namespace Top.Client.Models
{
    public class ModelInstance : IDisposable
    {
        private readonly string _path;

        private ModelStore _modelStore;

        internal ModelInstance(ModelStore modelStore, string path, Transform root)
        {
            _modelStore = modelStore;
            _path = path;

            Root = root;
        }

        public Transform Root { get; private set; }

        public void Dispose()
        {
            if (_modelStore == null)
            {
                return;
            }

            var store = _modelStore;

            _modelStore = null;

            if (Root != null)
            {
                UnityObjects.Destroy(Root.gameObject);
            }

            Root = null;

            store.Release(_path);
        }
    }
}
