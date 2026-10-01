using System.Numerics;
using NUnit.Framework;
using Top.Conversion.Models.Gltf;
using Top.Conversion.Gltf;
using Top.Legacy.MindPower.Geometry;

namespace Top.Conversion.Tests
{
    /// <summary>
    /// The MindPower half of the axis change, pinned to literals.
    /// <br/>
    /// The table shows the reference model below in both spaces. Read across a
    /// row to follow a value from MindPower to glTF.
    /// <code>
    ///           MindPower       glTF
    /// vertex    1, 2, 3         1, 3, 2
    /// normal    0.6, 0.8, 0     0.6, 0, 0.8
    /// uv        0.25, 0.75      0.25, 0.75
    /// triangle  0, 1, 2         0, 2, 1
    /// origin    10, 20, 30      10, 30, 20
    /// </code>
    /// </summary>
    public class AxisGoldenTests
    {
        private static GltfData Convert(out GltfDocument document)
        {
            var file = GltfExport.Object("reference", ReferenceModel());

            document = file.Document;

            return new GltfData(file);
        }

        private static GeometryObject ReferenceModel()
        {
            return new GeometryObject
            {
                Id = 0,
                ParentId = uint.MaxValue,
                LocalMatrix = Matrix4x4.CreateTranslation(10, 20, 30),
                Mesh = new Mesh
                {
                    PointType = 4,
                    Vertices = [new Vector3(1, 2, 3), new Vector3(0, 0, 0), new Vector3(0, 0, 0)],
                    Normals = [new Vector3(0.6f, 0.8f, 0f), Vector3.UnitZ, Vector3.UnitZ],
                    TextureCoordinates = [[new Vector2(0.25f, 0.75f), Vector2.Zero, Vector2.Zero]],
                    Indices = [0, 1, 2],
                    Subsets = [new MeshSubset { PrimitiveCount = 1, StartIndex = 0, VertexCount = 3 }],
                },
            };
        }

        [Test]
        public void The_reference_vertex_lands_on_its_glTF_numbers()
        {
            var data = Convert(out var document);
            var positions = data.ReadFloats(
                document.Meshes[0].Primitives[0].Attributes["POSITION"]);

            Assert.That(new[] { positions[0], positions[1], positions[2] },
                Is.EqualTo(new[] { 1f, 3f, 2f }));
        }

        [Test]
        public void The_reference_normal_lands_on_its_glTF_numbers()
        {
            var data = Convert(out var document);
            var normals = data.ReadFloats(document.Meshes[0].Primitives[0].Attributes["NORMAL"]);

            Assert.That(normals[0], Is.EqualTo(0.6f).Within(1e-6f));
            Assert.That(normals[1], Is.EqualTo(0f).Within(1e-6f));
            Assert.That(normals[2], Is.EqualTo(0.8f).Within(1e-6f));
        }

        [Test]
        public void The_reference_coordinate_crosses_unchanged()
        {
            var data = Convert(out var document);
            var uvs = data.ReadFloats(document.Meshes[0].Primitives[0].Attributes["TEXCOORD_0"]);

            Assert.That(new[] { uvs[0], uvs[1] }, Is.EqualTo(new[] { 0.25f, 0.75f }));
        }

        [Test]
        public void The_reference_triangle_winds_the_other_way()
        {
            var data = Convert(out var document);
            var indices = data.ReadInts(document.Meshes[0].Primitives[0].Indices.Value);

            Assert.That(indices, Is.EqualTo(new[] { 0, 2, 1 }));
        }

        [Test]
        public void The_reference_origin_lands_on_its_glTF_numbers()
        {
            Convert(out var document);

            var matrix = document.Nodes[0].Matrix;

            Assert.That(new[] { matrix[12], matrix[13], matrix[14] },
                Is.EqualTo(new[] { 10f, 30f, 20f }));
        }
    }
}
