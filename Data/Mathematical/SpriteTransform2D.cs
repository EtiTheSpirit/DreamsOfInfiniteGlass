using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.Data.Mathematical {

	/// <summary>
	/// A matrix indicating a sprite's rotation and position, row major.
	/// </summary>
	public readonly struct SpriteTransform2D : IEquatable<SpriteTransform2D> {

		public readonly float m00, m01;
		public readonly float m10, m11;
		public readonly float x, y;

		public static SpriteTransform2D Identity => new SpriteTransform2D(1, 0, 0, 1, 0, 0);

		/// <summary>
		/// The position of this matrix.
		/// </summary>
		public readonly Vector2 position;

		/// <summary>
		/// The scale of this matrix.
		/// </summary>
		public readonly Vector2 scale;

		/// <summary>
		/// The rotation of this matrix.
		/// </summary>
		public readonly float rotation;

		public SpriteTransform2D(Vector2 at) : this(at, 0, Vector2.one) { }

		/// <summary>
		/// Creates a new transformation at the provided location and rotation.
		/// <para/>
		/// The rotation is measured in radians about the unit circle (going counterclockwise).
		/// </summary>
		/// <param name="at"></param>
		/// <param name="rotationRads"></param>
		public SpriteTransform2D(Vector2 at, float rotationRads) : this(at, rotationRads, Vector2.one) { }

		/// <summary>
		/// Creates a new transformation at the provided location and rotation with the provided scale on both axes.
		/// <para/>
		/// The rotation is measured in radians about the unit circle (going counterclockwise).
		/// </summary>
		/// <param name="at"></param>
		/// <param name="rotationRads"></param>
		/// <param name="uniformScale"></param>
		public SpriteTransform2D(Vector2 at, float rotationRads, float uniformScale) : this(at, rotationRads, new Vector2(uniformScale, uniformScale)) { }

		/// <summary>
		/// Creates a new transformation at the provided location and rotation with the provided scale.
		/// <para/>
		/// The rotation is measured in radians about the unit circle (going counterclockwise).
		/// </summary>
		/// <param name="at"></param>
		/// <param name="rotationRads"></param>
		/// <param name="scale"></param>
		public SpriteTransform2D(Vector2 at, float rotationRads, Vector2 scale) {
			x = at.x;
			y = at.x;
			m00 = Mathf.Cos(rotationRads) * scale.x;
			m01 = -Mathf.Sin(rotationRads) * scale.x;
			m10 = Mathf.Sin(rotationRads) * scale.y;
			m11 = Mathf.Cos(rotationRads) * scale.y;

			position = new Vector2(x, y);
			rotation = rotationRads;
			this.scale = new Vector2(Mathf.Sqrt(m00 * m00 + m01 * m01), Mathf.Sqrt(m10 * m10 + m11 * m11));
		}

		/// <summary>
		/// Copy a <see cref="SpriteTransform2D"/>.
		/// </summary>
		/// <param name="other"></param>
		public SpriteTransform2D(SpriteTransform2D other) {
			x = other.x;
			y = other.y;
			m00 = other.m00;
			m01 = other.m01;
			m10 = other.m10;
			m11 = other.m11;

			position = other.position;
			rotation = other.rotation;
			scale = other.scale;
		}

		/// <summary>
		/// Construct a new <see cref="SpriteTransform2D"/> from the provided 2x2 rotation matrix and x, y coordinates.
		/// </summary>
		/// <param name="m00"></param>
		/// <param name="m01"></param>
		/// <param name="m10"></param>
		/// <param name="m11"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public SpriteTransform2D(float m00, float m01, float m10, float m11, float x, float y) {
			this.m00 = m00;
			this.m01 = m01;
			this.m10 = m10;
			this.m11 = m11;
			this.x = x;
			this.y = y;

			position = new Vector2(x, y);
			rotation = Mathf.Atan2(m10, m00);
			scale = new Vector2(Mathf.Sqrt(m00 * m00 + m01 * m01), Mathf.Sqrt(m10 * m10 + m11 * m11));
		}

		/// <summary>
		/// Construct a new <see cref="SpriteTransform2D"/> from the provided 2x2 rotation matrix and x, y coordinates.
		/// </summary>
		/// <param name="m00"></param>
		/// <param name="m01"></param>
		/// <param name="m10"></param>
		/// <param name="m11"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="negativeRotation">For internal use, negates the rotation</param>
		private SpriteTransform2D(float m00, float m01, float m10, float m11, float x, float y, bool negativeRotation) {
			this.m00 = m00;
			this.m01 = m01;
			this.m10 = m10;
			this.m11 = m11;
			this.x = x;
			this.y = y;

			position = new Vector2(x, y);
			rotation = Mathf.Abs(Mathf.Atan2(m10, m00)) * (negativeRotation ? -1 : 1);
			scale = new Vector2(Mathf.Sqrt(m00 * m00 + m01 * m01), Mathf.Sqrt(m10 * m10 + m11 * m11));
		}

		public SpriteTransform2D Invert() {
			float determinant = m00 * m11 - m01 * m10;
			if (determinant == 0) throw new ArithmeticException("This matrix is singular; cannot divide by zero.");

			float newM00 = m11 / determinant;
			float newM01 = -m01 / determinant;
			float newM10 = -m10 / determinant;
			float newM11 = m00 / determinant;

			Vector2 newOffset = this * new Vector2(-x, -y);

			return new SpriteTransform2D(
				newM00, newM01,
				newM10, newM11,
				newOffset.x, newOffset.y,
				rotation > 0
			);
		}

		/// <summary>
		/// Rotate this matrix by the provided amount in radians, going counterclockwise.
		/// </summary>
		/// <param name="radians"></param>
		public SpriteTransform2D Rotate(float radians) {
			return this * new SpriteTransform2D(default, radians);
		}

		/// <summary>
		/// Inverts this matrix.
		/// </summary>
		/// <param name="this"></param>
		/// <returns></returns>
		public static SpriteTransform2D operator ~(SpriteTransform2D @this) => @this.Invert();

		/// <summary>
		/// Transform the provided vector by this matrix.
		/// </summary>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static Vector2 operator *(SpriteTransform2D left, Vector2 right) {
			return new Vector2(
				left.m00 * right.x + left.m01 * right.y,
				left.m10 * right.x + left.m11 * right.y
			);
		}

		/// <summary>
		/// Translate this matrix locally by the provided vector.
		/// </summary>
		/// <param name="right"></param>
		/// <param name="left"></param>
		/// <returns></returns>
		public static SpriteTransform2D operator +(SpriteTransform2D left, Vector2 right) {
			Vector2 offset = left * right;
			return new SpriteTransform2D(
				left.m00, left.m01,
				left.m10, left.m11,

				left.x + offset.x,
				left.y + offset.y
			);
		}

		public static SpriteTransform2D operator *(SpriteTransform2D left, SpriteTransform2D right) {
			// Get the dot product of the *x*th row and *y*th column to find the answer for mXY in the result.
			// (m00, m01) • (m00, m10)
			// (m00, m01) • (m10, m11)
			// (m10, m11) • (m00, m10)
			// (m10, m11) • (m10, m11)
			float newM00 = left.m00 * right.m00 + left.m01 * right.m10;
			float newM01 = left.m00 * right.m10 + left.m01 * right.m11;
			float newM10 = left.m10 * right.m00 + left.m11 * right.m10;
			float newM11 = left.m10 * right.m10 + left.m11 * right.m11;
			Vector2 offset = left * right.position;

			return new SpriteTransform2D(
				newM00, newM01,
				newM10, newM11,

				left.x + offset.x,
				left.y + offset.y
			);
		}

		public static SpriteTransform2D operator /(SpriteTransform2D left, SpriteTransform2D right) {
			return ~left * right;
		}

		public static bool operator ==(SpriteTransform2D left, SpriteTransform2D right) => left.Equals(right);

		public static bool operator !=(SpriteTransform2D left, SpriteTransform2D right) => !(left == right);

		public override string ToString() {
			return $"[[{m00}, {m01}], [{m10}, {m11}]] ({x}, {y})";
		}

		public override bool Equals(object obj) {
			if (obj is SpriteTransform2D transform) return Equals(transform);
			return false;
		}

		public override int GetHashCode() {
			return unchecked(((F32ToI(m00) + F32ToI(m01)) ^ (F32ToI(m10) + F32ToI(m11))) + F32ToI(x) ^ F32ToI(y));
		}

		private static unsafe int F32ToI(float f) {
			return *(int*)(&f);
		}

		public bool Equals(SpriteTransform2D other) {
			if (!FuzzyEq(m00, other.m00)) return false;
			if (!FuzzyEq(m01, other.m01)) return false;
			if (!FuzzyEq(m10, other.m10)) return false;
			if (!FuzzyEq(m11, other.m11)) return false;
			if (!FuzzyEq(x, other.x)) return false;
			if (!FuzzyEq(y, other.y)) return false;
			return true;
		}

		private static bool FuzzyEq(float left, float right) {
			return Mathf.Abs(left - right) < 1e-5f;
		}
	}
}
