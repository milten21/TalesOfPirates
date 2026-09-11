using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Game.World.SceneObjects;
using Top.Client.Models;
using Top.Contracts.Tables.World;
using UnityEngine;

namespace Top.Client.Game.Tests.World.SceneObjects
{
    public class SceneObjectFactoryTests
    {
        private class RecordingModels : IModelStore
        {
            public readonly List<string> Requested = new List<string>();

            public Task<ModelInstance> Instantiate(string path, Transform parent,
                CancellationToken cancellationToken = default)
            {
                Requested.Add(path);

                return Task.FromResult<ModelInstance>(null);
            }

            public Task EnsureLoaded(IEnumerable<string> paths)
            {
                return Task.CompletedTask;
            }
        }

        private static (SceneObjectFactory Factory, RecordingModels Models) CreateFactory(
            params SceneObjectEntry[] entries)
        {
            var models = new RecordingModels();

            return (new SceneObjectFactory(new SceneObjectTable(entries), models), models);
        }

        [Test]
        public async Task An_entry_instantiates_the_model_its_row_names()
        {
            var (factory, models) = CreateFactory(new SceneObjectEntry { Id = 42, ModelPath = "models/scene/stone01.glb" });

            await factory.Instantiate(42, null);

            Assert.That(models.Requested, Is.EqualTo(new[] { "models/scene/stone01.glb" }));
        }

        [Test]
        public async Task An_id_the_table_does_not_name_instantiates_nothing()
        {
            var (factory, models) = CreateFactory(new SceneObjectEntry { Id = 42, ModelPath = "models/scene/stone01.glb" });

            Assert.That(await factory.Instantiate(9000, null), Is.Null);
            Assert.That(models.Requested, Is.Empty);
        }

        [Test]
        public async Task A_helper_entry_never_instantiates_its_placeholder_model()
        {
            var (factory, models) = CreateFactory(new SceneObjectEntry
                { Id = 353, Kind = SceneObjectKind.Passability, ModelPath = "models/scene/yyyy003.glb" });

            Assert.That(await factory.Instantiate(353, null), Is.Null,
                "the original draws only normal-type scene objects, helper types are logic markers");
            Assert.That(models.Requested, Is.Empty);
        }

        [Test]
        public async Task An_entry_naming_no_model_instantiates_nothing()
        {
            var (factory, models) = CreateFactory(new SceneObjectEntry { Id = 7 });

            Assert.That(await factory.Instantiate(7, null), Is.Null);
            Assert.That(models.Requested, Is.Empty);
        }
    }
}
