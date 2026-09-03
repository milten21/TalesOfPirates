using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Top.Client.Models.Gltf;
using Top.Content;
using UnityEngine;

namespace Top.Client.Models.Tests
{
    public class ContentDownloadProviderTests
    {
        private const string ModelPath = "models/scene/my_bd001.glb";
        private const string TexturePath = "textures/scene/010297.png";
        private const string JsonModelPath = "models/scene/my_bd002.gltf";
        private const string Json = "{\"asset\":{}}";
        private const uint GltfBinaryMagic = 0x46546c67;

        private byte[] _glb;
        private byte[] _png;
        private ContentDownloadProvider _provider;

        [SetUp]
        public void Setup()
        {
            _glb = Glb();
            _png = Png();

            var content = new MemoryContentSource();

            content.Add(ModelPath, _glb);
            content.Add(TexturePath, _png);
            content.Add(JsonModelPath, Encoding.UTF8.GetBytes(Json));

            _provider = new ContentDownloadProvider(content);
        }

        [Test]
        public async Task A_model_comes_back_as_the_bytes_the_content_holds()
        {
            var download = await _provider.Request(ContentDownloadProvider.UriFor(ModelPath));

            Assert.That(download.Success, Is.True);
            Assert.That(download.Data, Is.EqualTo(_glb));
            Assert.That(download.IsBinary, Is.True);
        }

        [Test]
        public async Task A_texture_uri_relative_to_the_model_resolves()
        {
            var model = ContentDownloadProvider.UriFor(ModelPath);

            var download = await _provider.Request(new Uri(model, "../../textures/scene/010297.png"));

            Assert.That(download.Success, Is.True);
            Assert.That(download.Data, Is.EqualTo(_png));
        }

        [Test]
        public async Task A_uri_carrying_windows_separators_resolves()
        {
            var download = await _provider.Request(new Uri(@"top://content/textures\scene\010297.png"));

            Assert.That(download.Success, Is.True);
            Assert.That(download.Data, Is.EqualTo(_png));
        }

        [Test]
        public async Task A_uri_whose_case_differs_from_the_stored_name_resolves()
        {
            var download = await _provider.Request(ContentDownloadProvider.UriFor("Textures/Scene/010297.PNG"));

            Assert.That(download.Success, Is.True);
            Assert.That(download.Data, Is.EqualTo(_png));
        }

        [Test]
        public async Task A_path_the_content_does_not_hold_fails()
        {
            var download = await _provider.Request(ContentDownloadProvider.UriFor("models/scene/absent.glb"));

            Assert.That(download.Success, Is.False);
            Assert.That(download.Error, Is.Not.Null.And.Not.Empty);
            Assert.That(download.Data, Is.Null);
        }

        [Test]
        public async Task A_path_breaking_the_content_rules_fails()
        {
            var download = await _provider.Request(new Uri("top://content/models//absent.glb"));

            Assert.That(download.Success, Is.False);
            Assert.That(download.Error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task A_read_that_breaks_fails_the_download()
        {
            var provider = new ContentDownloadProvider(new BrokenContent());

            var download = await provider.Request(ContentDownloadProvider.UriFor(ModelPath));

            Assert.That(download.Success, Is.False);
            Assert.That(download.Error, Does.Contain("the disk went away"));
        }

        [Test]
        public async Task A_uri_outside_the_content_scheme_fails()
        {
            var download = await _provider.Request(new Uri("https://example.com/models/scene/my_bd001.glb"));

            Assert.That(download.Success, Is.False);
            Assert.That(download.Error, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task A_json_model_comes_back_as_text()
        {
            var download = await _provider.Request(ContentDownloadProvider.UriFor(JsonModelPath));

            Assert.That(download.Text, Is.EqualTo(Json));
            Assert.That(download.IsBinary, Is.False);
        }

        [Test]
        public async Task A_texture_request_decodes_the_png()
        {
            var download = await _provider.RequestTexture(ContentDownloadProvider.UriFor(TexturePath), false);

            Assert.That(download.Success, Is.True);
            Assert.That(download.Texture, Is.Not.Null);
            Assert.That(download.Texture.width, Is.EqualTo(4));
            Assert.That(download.Texture.height, Is.EqualTo(4));
        }

        [Test]
        public async Task A_texture_the_content_does_not_hold_fails_without_a_texture()
        {
            var download = await _provider.RequestTexture(
                ContentDownloadProvider.UriFor("textures/scene/absent.png"), false);

            Assert.That(download.Success, Is.False);
            Assert.That(download.Texture, Is.Null);
        }

        private static byte[] Glb()
        {
            var bytes = new byte[12];

            BitConverter.GetBytes(GltfBinaryMagic).CopyTo(bytes, 0);

            return bytes;
        }

        private static byte[] Png()
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);

            texture.SetPixel(0, 0, Color.red);
            texture.Apply();

            return texture.EncodeToPNG();
        }

        private class BrokenContent : IContentSource
        {
            public Task<byte[]> Read(string path)
            {
                throw new IOException("the disk went away");
            }

            public bool Exists(string path)
            {
                return true;
            }

            public IEnumerable<string> List(string prefix)
            {
                return Array.Empty<string>();
            }
        }
    }
}
