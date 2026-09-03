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

            public Task<ModelInstance> Spawn(string path, Transform parent,
                CancellationToken cancel = default)
            {
                Requested.Add(path);

                return Task.FromResult<ModelInstance>(null);
            }

            public Task Preload(IEnumerable<string> paths)
            {
                return Task.CompletedTask;
            }
        }

        private static (SceneObjectFactory Factory, RecordingModels Models) Factory(
            params SceneObjectEntry[] entries)
        {
            var models = new RecordingModels();

            return (new SceneObjectFactory(new SceneObjectTable(entries), models), models);
        }

        [Test]
        public async Task An_entry_is_built_from_the_model_its_row_names()
        {
            var (factory, models) = Factory(new SceneObjectEntry { Id = 42, ModelPath = "models/scene/stone01.glb" });

            await factory.Build(42, null);

            Assert.That(models.Requested, Is.EqualTo(new[] { "models/scene/stone01.glb" }));
        }

        [Test]
        public async Task An_id_the_table_does_not_name_builds_nothing()
        {
            var (factory, models) = Factory(new SceneObjectEntry { Id = 42, ModelPath = "models/scene/stone01.glb" });

            Assert.That(await factory.Build(9000, null), Is.Null);
            Assert.That(models.Requested, Is.Empty);
        }

        [Test]
        public async Task A_helper_entry_never_builds_its_placeholder_model()
        {
            var (factory, models) = Factory(new SceneObjectEntry
                { Id = 353, Kind = SceneObjectKind.Passability, ModelPath = "models/scene/yyyy003.glb" });

            Assert.That(await factory.Build(353, null), Is.Null,
                "the original draws only normal-type scene objects, helper types are logic markers");
            Assert.That(models.Requested, Is.Empty);
        }

        [Test]
        public async Task An_entry_naming_no_model_builds_nothing()
        {
            var (factory, models) = Factory(new SceneObjectEntry { Id = 7 });

            Assert.That(await factory.Build(7, null), Is.Null);
            Assert.That(models.Requested, Is.Empty);
        }
    }
}
