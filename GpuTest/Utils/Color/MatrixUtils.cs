namespace GpuTest.Utils.Color;

internal static class MatrixUtils
{
    /// <summary>
    /// Matrix inverse for 3 by 3 matrices.
    /// </summary>
    public static Matrix3x3 Inverse(in Matrix3x3 matrix)
    {
        // var A = matrix[1, 1] * matrix[2, 2] - matrix[1, 2] * matrix[2, 1];
        var A = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
        // var D = -(matrix[0, 1] * matrix[2, 2] - matrix[0, 2] * matrix[2, 1]);
        var D = -(matrix.M01 * matrix.M22 - matrix.M02 * matrix.M21);
        //  var G = matrix[0, 1] * matrix[1, 2] - matrix[0, 2] * matrix[1, 1];
        var G = matrix.M01 * matrix.M12 - matrix.M02 * matrix.M11;
        // var B = -(matrix[1, 0] * matrix[2, 2] - matrix[1, 2] * matrix[2, 0]);
        var B = -(matrix.M10 * matrix.M22 - matrix.M12 * matrix.M20);
        // var E = matrix[0, 0] * matrix[2, 2] - matrix[0, 2] * matrix[2, 0];
        var E = matrix.M00 * matrix.M22 - matrix.M02 * matrix.M20;
        // var H = -(matrix[0, 0] * matrix[1, 2] - matrix[0, 2] * matrix[1, 0]);
        var H = -(matrix.M00 * matrix.M12 - matrix.M02 * matrix.M10);
        // var C = matrix[1, 0] * matrix[2, 1] - matrix[1, 1] * matrix[2, 0];
        var C = matrix.M10 * matrix.M21 - matrix.M11 * matrix.M20;
        // var F = -(matrix[0, 0] * matrix[2, 1] - matrix[0, 1] * matrix[2, 0]);
        var F = -(matrix.M00 * matrix.M21 - matrix.M01 * matrix.M20);
        // var I = matrix[0, 0] * matrix[1, 1] - matrix[0, 1] * matrix[1, 0];
        var I = matrix.M00 * matrix.M11 - matrix.M01 * matrix.M10;
        // var det = matrix[0, 0] * A + matrix[0, 1] * B + matrix[0, 2] * C;
        var det = matrix.M00 * A + matrix.M01 * B + matrix.M02 * C;

        Matrix3x3 result = new Matrix3x3(
            A / det, D / det, G / det ,
            B / det, E / det, H / det ,
            C / det, F / det, I / det 
        );
        return result;
    }

    public static Vec3 MultiplyBy(in Matrix3x3 matrix, in Vec3 vector)
    {
        Vec3 result = new Vec3();
 
        result.X += matrix.M00 * vector.X;
        result.X += matrix.M01 * vector.Y;
        result.X += matrix.M02 * vector.Z;

        result.Y += matrix.M10 * vector.X;
        result.Y += matrix.M11 * vector.Y;
        result.Y += matrix.M12 * vector.Z;

        result.Z += matrix.M20 * vector.X;
        result.Z += matrix.M21 * vector.Y;
        result.Z += matrix.M22 * vector.Z;

        return result;
    }
}
