using System;
using System.Numerics;

namespace Top.Conversion.Models.Gltf
{
    /// <summary>
    /// The translation, rotation and scale one glTF node holds. A node scales
    /// along its own axes only, so it holds a matrix whose rows are
    /// perpendicular exactly, and any other matrix - shear, or a scale off its
    /// axes - as the nearest it can.
    /// </summary>
    public readonly struct Trs
    {
        private const float PerpendicularTolerance = 1e-4f;
        private const float EmptyAxis = 1e-6f;
        private const float Converged = 1e-7f;
        private const int FitSteps = 20;
        private const int PolarSteps = 50;

        public static readonly Trs Identity = new Trs(Vector3.Zero, Quaternion.Identity, Vector3.One);

        public Trs(Vector3 translation, Quaternion rotation, Vector3 scale)
        {
            Translation = translation;
            Rotation = rotation;
            Scale = scale;
        }

        public Vector3 Translation { get; }

        public Quaternion Rotation { get; }

        public Vector3 Scale { get; }

        public Matrix4x4 Matrix => Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateFromQuaternion(Rotation)
                                                                * Matrix4x4.CreateTranslation(Translation);

        /// <summary>
        /// The node transform nearest to a matrix, least squares over its
        /// linear part, split as Godot splits a bone's rest: a mirrored matrix
        /// takes its negative scale on all three axes. Godot blends a clip
        /// relative to this rotation.
        /// </summary>
        public static Trs Of(Matrix4x4 matrix)
        {
            var signs = IsMirrored(matrix) ? -Vector3.One : Vector3.One;

            return Split(matrix, signs, Quaternion.Identity);
        }

        /// <summary>
        /// The node transform nearest to a matrix, least squares over its
        /// linear part, with the scale signs of <paramref name="near"/>. Where
        /// the matrix mirrors and <paramref name="near"/> does not, or the
        /// reverse, one axis changes its sign: the one that leaves the
        /// rotation nearest to <paramref name="near"/>'s. The rotation takes
        /// the sign nearest to it too, so that a sequence of keys interpolates
        /// without a flip, and a blend from the rest does not flip either.
        /// </summary>
        public static Trs Of(Matrix4x4 matrix, Trs near)
        {
            var signs = Signs(near.Scale);

            if (IsMirrored(matrix) == IsMirrored(signs))
            {
                return Split(matrix, signs, near.Rotation);
            }

            var best = Identity;
            var bestDot = float.NegativeInfinity;

            for (var flipped = 0; flipped < 3; flipped++)
            {
                var flippedSigns = signs;

                flippedSigns[flipped] = -flippedSigns[flipped];

                var candidate = Split(matrix, flippedSigns, near.Rotation);
                var dot = Quaternion.Dot(candidate.Rotation, near.Rotation);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = candidate;
                }
            }

            return best;
        }

        private static Trs Split(Matrix4x4 matrix, Vector3 signs, Quaternion nearRotation)
        {
            var rows = new[] { Row(matrix, 0), Row(matrix, 1), Row(matrix, 2) };
            var scale = new Vector3(rows[0].Length(), rows[1].Length(), rows[2].Length()) * signs;

            return new Trs(matrix.Translation, Fit(rows, ref scale, nearRotation), scale);
        }

        private static bool IsMirrored(Matrix4x4 matrix)
        {
            return matrix.GetDeterminant() < 0f;
        }

        private static bool IsMirrored(Vector3 signs)
        {
            return signs.X * signs.Y * signs.Z < 0f;
        }

        private static Vector3 Signs(Vector3 scale)
        {
            return new Vector3(scale.X < 0f ? -1f : 1f, scale.Y < 0f ? -1f : 1f, scale.Z < 0f ? -1f : 1f);
        }

        private static Vector3 Row(Matrix4x4 matrix, int row)
        {
            return new Vector3(matrix[row, 0], matrix[row, 1], matrix[row, 2]);
        }

        /// <summary>
        /// Alternates the rotation nearest to the rows at a fixed scale with
        /// the scale nearest to them at a fixed rotation. Rows that are
        /// perpendicular already give the exact answer in the first step.
        /// </summary>
        private static Quaternion Fit(Vector3[] rows, ref Vector3 scale, Quaternion near)
        {
            var axes = Axes(rows, scale);

            if (ArePerpendicular(axes))
            {
                return Hemisphere(RotationOf(axes), near);
            }

            var rotation = axes;

            for (var step = 0; step < FitSteps; step++)
            {
                rotation = NearestRotation(Weighted(Axes(rows, scale), scale));

                var next = new Vector3(Vector3.Dot(rows[0], rotation[0]), Vector3.Dot(rows[1], rotation[1]),
                    Vector3.Dot(rows[2], rotation[2]));
                var done = Vector3.Distance(next, scale) < Converged;

                scale = next;

                if (done)
                {
                    break;
                }
            }

            return Hemisphere(RotationOf(rotation), near);
        }

        private static bool ArePerpendicular(Vector3[] axes)
        {
            return MathF.Abs(Vector3.Dot(axes[0], axes[1])) <= PerpendicularTolerance
                   && MathF.Abs(Vector3.Dot(axes[0], axes[2])) <= PerpendicularTolerance
                   && MathF.Abs(Vector3.Dot(axes[1], axes[2])) <= PerpendicularTolerance;
        }

        /// <summary>
        /// The rows divided by their signed scale. A row scaled to nothing
        /// takes the direction the others leave free.
        /// </summary>
        private static Vector3[] Axes(Vector3[] rows, Vector3 scale)
        {
            var axes = new Vector3[3];
            var empty = 0;

            for (var i = 0; i < 3; i++)
            {
                if (MathF.Abs(scale[i]) < EmptyAxis)
                {
                    empty++;
                }
                else
                {
                    axes[i] = rows[i] / scale[i];
                }
            }

            if (empty == 3)
            {
                return [Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];
            }

            if (empty == 2)
            {
                var kept = Array.FindIndex(axes, axis => axis != Vector3.Zero);
                var other = MathF.Abs(axes[kept].X) < 0.9f ? Vector3.UnitX : Vector3.UnitY;

                axes[(kept + 1) % 3] = Vector3.Normalize(Vector3.Cross(Vector3.Cross(axes[kept], other), axes[kept]));
            }

            for (var i = 0; i < 3; i++)
            {
                if (axes[i] == Vector3.Zero)
                {
                    axes[i] = Vector3.Normalize(Vector3.Cross(axes[(i + 1) % 3], axes[(i + 2) % 3]));
                }
            }

            return axes;
        }

        /// <summary>
        /// The axes times their squared scale: the rows of the matrix times
        /// the scale, whose nearest rotation is the least squares one. An
        /// axis scaled to nothing keeps its free direction.
        /// </summary>
        private static Vector3[] Weighted(Vector3[] axes, Vector3 scale)
        {
            var weighted = new Vector3[3];

            for (var i = 0; i < 3; i++)
            {
                weighted[i] = MathF.Abs(scale[i]) < EmptyAxis ? axes[i] : axes[i] * scale[i] * scale[i];
            }

            return weighted;
        }

        /// <summary>
        /// The rotation nearest to the rows, by averaging the matrix with its
        /// inverse transpose until it stops changing.
        /// </summary>
        private static Vector3[] NearestRotation(Vector3[] axes)
        {
            var current = RowMatrix(axes);

            for (var step = 0; step < PolarSteps; step++)
            {
                if (!Matrix4x4.Invert(current, out var inverse))
                {
                    break;
                }

                var next = (current + Matrix4x4.Transpose(inverse)) * 0.5f;
                var change = 0f;

                for (var i = 0; i < 3; i++)
                {
                    change = MathF.Max(change, Vector3.Distance(Row(next, i), Row(current, i)));
                }

                current = next;

                if (change < Converged)
                {
                    break;
                }
            }

            return [Row(current, 0), Row(current, 1), Row(current, 2)];
        }

        private static Matrix4x4 RowMatrix(Vector3[] axes)
        {
            return new Matrix4x4(
                axes[0].X, axes[0].Y, axes[0].Z, 0f,
                axes[1].X, axes[1].Y, axes[1].Z, 0f,
                axes[2].X, axes[2].Y, axes[2].Z, 0f,
                0f, 0f, 0f, 1f);
        }

        private static Quaternion RotationOf(Vector3[] axes)
        {
            return Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(RowMatrix(axes)));
        }

        private static Quaternion Hemisphere(Quaternion rotation, Quaternion near)
        {
            return Quaternion.Dot(rotation, near) < 0f
                ? new Quaternion(-rotation.X, -rotation.Y, -rotation.Z, -rotation.W)
                : rotation;
        }
    }
}
