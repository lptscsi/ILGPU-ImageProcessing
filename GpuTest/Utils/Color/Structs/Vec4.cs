using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GpuTest.Utils.Color
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public partial struct Vec4 : IEquatable<Vec4>
    {
        public const int SIZE = sizeof(float) * 4;

        /// <summary>The X component of the vector.</summary>
        public float X;

        /// <summary>The Y component of the vector.</summary>
        public float Y;

        /// <summary>The Z component of the vector.</summary>
        public float Z;

        /// <summary>The W component of the vector.</summary>
        public float W;

        public Vec4(float value) : this(value, value, value, value)
        {
        }

        public Vec4(Vector2 value, float z, float w) : this(value.X, value.Y, z, w)
        {
        }

        public Vec4(Vector3 value, float w) : this(value.X, value.Y, value.Z, w)
        {
        }

        /// <summary>Creates a vector whose elements have the specified values.</summary>
        /// <param name="x">The value to assign to the <see cref="Vec4.X" /> field.</param>
        /// <param name="y">The value to assign to the <see cref="Vec4.Y" /> field.</param>
        /// <param name="z">The value to assign to the <see cref="Vec4.Z" /> field.</param>
        /// <param name="w">The value to assign to the <see cref="Vec4.W" /> field.</param>
      
        public Vec4(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        /*
        public Vec4(ReadOnlySpan<float> values)
        {
            this = Unsafe.ReadUnaligned<Vec4>(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(values)));
        }
        */

        /// <summary>Gets a vector whose 4 elements are equal to zero.</summary>
        /// <value>A vector whose four elements are equal to zero (that is, it returns the vector <c>(0,0,0,0)</c>.</value>
        public static Vec4 Zero
        {
          
            get => default;
        }

        /// <summary>Gets a vector whose 4 elements are equal to one.</summary>
        /// <value>Returns <see cref="Vec4" />.</value>
        /// <remarks>A vector whose four elements are equal to one (that is, it returns the vector <c>(1,1,1,1)</c>.</remarks>
        public static Vec4 One
        {
          
            get => new Vec4(1.0f);
        }

        /// <summary>Gets the vector (1,0,0,0).</summary>
        /// <value>The vector <c>(1,0,0,0)</c>.</value>
        public static Vec4 UnitX
        {
            get => new Vec4(1.0f, 0.0f, 0.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,1,0,0).</summary>
        /// <value>The vector <c>(0,1,0,0)</c>.</value>
        public static Vec4 UnitY
        {
            get => new Vec4(0.0f, 1.0f, 0.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,1,0).</summary>
        /// <value>The vector <c>(0,0,1,0)</c>.</value>
        public static Vec4 UnitZ
        {
            get => new Vec4(0.0f, 0.0f, 1.0f, 0.0f);
        }

        /// <summary>Gets the vector (0,0,0,1).</summary>
        /// <value>The vector <c>(0,0,0,1)</c>.</value>
        public static Vec4 UnitW
        {
            get => new Vec4(0.0f, 0.0f, 0.0f, 1.0f);
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        /// <remarks>The <see cref="Vec4.op_Addition" /> method defines the addition operation for <see cref="Vec4" /> objects.</remarks>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator +(Vec4 left, Vec4 right)
        {
            return new Vec4(
                left.X + right.X,
                left.Y + right.Y,
                left.Z + right.Z,
                left.W + right.W
            );
        }

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from dividing <paramref name="left" /> by <paramref name="right" />.</returns>
        /// <remarks>The <see cref="Vec4.op_Division" /> method defines the division operation for <see cref="Vec4" /> objects.</remarks>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator /(Vec4 left, Vec4 right)
        {
            return new Vec4(
                left.X / right.X,
                left.Y / right.Y,
                left.Z / right.Z,
                left.W / right.W
            );
        }

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="value1">The vector.</param>
        /// <param name="value2">The scalar value.</param>
        /// <returns>The result of the division.</returns>
        /// <remarks>The <see cref="Vec4.op_Division" /> method defines the division operation for <see cref="Vec4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator /(Vec4 value1, float value2)
        {
            return value1 / new Vec4(value2);
        }

        /// <summary>Returns a value that indicates whether each pair of elements in two specified vectors is equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two <see cref="Vec4" /> objects are equal if each element in <paramref name="left" /> is equal to the corresponding element in <paramref name="right" />.</remarks>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vec4 left, Vec4 right)
        {
            return (left.X == right.X)
                && (left.Y == right.Y)
                && (left.Z == right.Z)
                && (left.W == right.W);
        }

        /// <summary>Returns a value that indicates whether two specified vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns><see langword="true" /> if <paramref name="left" /> and <paramref name="right" /> are not equal; otherwise, <see langword="false" />.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vec4 left, Vec4 right)
        {
            return !(left == right);
        }

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        /// <remarks>The <see cref="Vec4.op_Multiply" /> method defines the multiplication operation for <see cref="Vec4" /> objects.</remarks>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator *(Vec4 left, Vec4 right)
        {
            return new Vec4(
                left.X * right.X,
                left.Y * right.Y,
                left.Z * right.Z,
                left.W * right.W
            );
        }

        /// <summary>Multiplies the specified vector by the specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vec4.op_Multiply" /> method defines the multiplication operation for <see cref="Vec4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator *(Vec4 left, float right)
        {
            return left * new Vec4(right);
        }

        /// <summary>Multiplies the scalar value by the specified vector.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        /// <remarks>The <see cref="Vec4.op_Multiply" /> method defines the multiplication operation for <see cref="Vec4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator *(float left, Vec4 right)
        {
            return right * left;
        }

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector that results from subtracting <paramref name="right" /> from <paramref name="left" />.</returns>
        /// <remarks>The <see cref="Vec4.op_Subtraction" /> method defines the subtraction operation for <see cref="Vec4" /> objects.</remarks>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator -(Vec4 left, Vec4 right)
        {
            return new Vec4(
                left.X - right.X,
                left.Y - right.Y,
                left.Z - right.Z,
                left.W - right.W
            );
        }

        /// <summary>Negates the specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        /// <remarks>The <see cref="Vec4.op_UnaryNegation" /> method defines the unary negation operation for <see cref="Vec4" /> objects.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 operator -(Vec4 value)
        {
            return Zero - value;
        }

        /// <summary>Returns a vector whose elements are the absolute values of each of the specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The absolute value vector.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Abs(Vec4 value)
        {
            return new Vec4(
                MathF.Abs(value.X),
                MathF.Abs(value.Y),
                MathF.Abs(value.Z),
                MathF.Abs(value.W)
            );
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first vector to add.</param>
        /// <param name="right">The second vector to add.</param>
        /// <returns>The summed vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Add(Vec4 left, Vec4 right)
        {
            return left + right;
        }

        /// <summary>Restricts a vector between a minimum and a maximum value.</summary>
        /// <param name="value1">The vector to restrict.</param>
        /// <param name="min">The minimum value.</param>
        /// <param name="max">The maximum value.</param>
        /// <returns>The restricted vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Clamp(Vec4 value1, Vec4 min, Vec4 max)
        {
            // We must follow HLSL behavior in the case user specified min value is bigger than max value.
            return Min(Max(value1, min), max);
        }

        /// <summary>Computes the Euclidean distance between the two given points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Distance(Vec4 value1, Vec4 value2)
        {
            float distanceSquared = DistanceSquared(value1, value2);
            return MathF.Sqrt(distanceSquared);
        }

        /// <summary>Returns the Euclidean distance squared between two specified points.</summary>
        /// <param name="value1">The first point.</param>
        /// <param name="value2">The second point.</param>
        /// <returns>The distance squared.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSquared(Vec4 value1, Vec4 value2)
        {
            Vec4 difference = value1 - value2;
            return Dot(difference, difference);
        }

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Divide(Vec4 left, Vec4 right)
        {
            return left / right;
        }

        /// <summary>Divides the specified vector by a specified scalar value.</summary>
        /// <param name="left">The vector.</param>
        /// <param name="divisor">The scalar value.</param>
        /// <returns>The vector that results from the division.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Divide(Vec4 left, float divisor)
        {
            return left / divisor;
        }

        /// <summary>Returns the dot product of two vectors.</summary>
        /// <param name="vector1">The first vector.</param>
        /// <param name="vector2">The second vector.</param>
        /// <returns>The dot product.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dot(Vec4 vector1, Vec4 vector2)
        {
            return (vector1.X * vector2.X)
                 + (vector1.Y * vector2.Y)
                 + (vector1.Z * vector2.Z)
                 + (vector1.W * vector2.W);
        }

        /// <summary>Performs a linear interpolation between two vectors based on the given weighting.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <param name="amount">A value between 0 and 1 that indicates the weight of <paramref name="value2" />.</param>
        /// <returns>The interpolated vector.</returns>
        /// <remarks><format type="text/markdown"><![CDATA[
        /// The behavior of this method changed in .NET 5.0. For more information, see [Behavior change for Vector2.Lerp and Vec4.Lerp](/dotnet/core/compatibility/3.1-5.0#behavior-change-for-vector2lerp-and-Vec4lerp).
        /// ]]></format></remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Lerp(Vec4 value1, Vec4 value2, float amount)
        {
            return (value1 * (1.0f - amount)) + (value2 * amount);
        }

        /// <summary>Returns a vector whose elements are the maximum of each of the pairs of elements in two specified vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The maximized vector.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Max(Vec4 value1, Vec4 value2)
        {
            return new Vec4(
                (value1.X > value2.X) ? value1.X : value2.X,
                (value1.Y > value2.Y) ? value1.Y : value2.Y,
                (value1.Z > value2.Z) ? value1.Z : value2.Z,
                (value1.W > value2.W) ? value1.W : value2.W
            );
        }

        /// <summary>Returns a vector whose elements are the minimum of each of the pairs of elements in two specified vectors.</summary>
        /// <param name="value1">The first vector.</param>
        /// <param name="value2">The second vector.</param>
        /// <returns>The minimized vector.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Min(Vec4 value1, Vec4 value2)
        {
            return new Vec4(
                (value1.X < value2.X) ? value1.X : value2.X,
                (value1.Y < value2.Y) ? value1.Y : value2.Y,
                (value1.Z < value2.Z) ? value1.Z : value2.Z,
                (value1.W < value2.W) ? value1.W : value2.W
            );
        }

        /// <summary>Returns a new vector whose values are the product of each pair of elements in two specified vectors.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The element-wise product vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Multiply(Vec4 left, Vec4 right)
        {
            return left * right;
        }

        /// <summary>Multiplies a vector by a specified scalar.</summary>
        /// <param name="left">The vector to multiply.</param>
        /// <param name="right">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Multiply(Vec4 left, float right)
        {
            return left * right;
        }

        /// <summary>Multiplies a scalar value by a specified vector.</summary>
        /// <param name="left">The scaled value.</param>
        /// <param name="right">The vector.</param>
        /// <returns>The scaled vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Multiply(float left, Vec4 right)
        {
            return left * right;
        }

        /// <summary>Negates a specified vector.</summary>
        /// <param name="value">The vector to negate.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Negate(Vec4 value)
        {
            return -value;
        }

        /// <summary>Returns a vector with the same direction as the specified vector, but with a length of one.</summary>
        /// <param name="vector">The vector to normalize.</param>
        /// <returns>The normalized vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Normalize(Vec4 vector)
        {
            return vector / vector.Length();
        }

        /// <summary>Returns a vector whose elements are the square root of each of a specified vector's elements.</summary>
        /// <param name="value">A vector.</param>
        /// <returns>The square root vector.</returns>
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 SquareRoot(Vec4 value)
        {
            return new Vec4(
                MathF.Sqrt(value.X),
                MathF.Sqrt(value.Y),
                MathF.Sqrt(value.Z),
                MathF.Sqrt(value.W)
            );
        }

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first vector.</param>
        /// <param name="right">The second vector.</param>
        /// <returns>The difference vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vec4 Subtract(Vec4 left, Vec4 right)
        {
            return left - right;
        }
     
        /// <summary>Returns a value that indicates whether this instance and another vector are equal.</summary>
        /// <param name="other">The other vector.</param>
        /// <returns><see langword="true" /> if the two vectors are equal; otherwise, <see langword="false" />.</returns>
        /// <remarks>Two vectors are equal if their <see cref="Vec4.X" />, <see cref="Vec4.Y" />, <see cref="Vec4.Z" />, and <see cref="Vec4.W" /> elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Vec4 other)
        {
            return this.X.Equals(other.X)
                  && this.Y.Equals(other.Y)
                  && this.Z.Equals(other.Z)
                  && this.W.Equals(other.W);
        }

        /// <summary>Returns a value that indicates whether this instance and a specified object are equal.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><see langword="true" /> if the current instance and <paramref name="obj" /> are equal; otherwise, <see langword="false" />. If <paramref name="obj" /> is <see langword="null" />, the method returns <see langword="false" />.</returns>
        /// <remarks>The current instance and <paramref name="obj" /> are equal if <paramref name="obj" /> is a <see cref="Vec4" /> object and their corresponding elements are equal.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object obj)
        {
            return (obj is Vec4 other) && Equals(other);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }

        /// <summary>Returns the length of this vector object.</summary>
        /// <returns>The vector's length.</returns>
        /// <altmember cref="Vec4.LengthSquared"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float Length()
        {
            float lengthSquared = LengthSquared();
            return MathF.Sqrt(lengthSquared);
        }

        /// <summary>Returns the length of the vector squared.</summary>
        /// <returns>The vector's length squared.</returns>
        /// <remarks>This operation offers better performance than a call to the <see cref="Vec4.Length" /> method.</remarks>
        /// <altmember cref="Vec4.Length"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly float LengthSquared()
        {
            return Dot(this, this);
        }
    }
}
