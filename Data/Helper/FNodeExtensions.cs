using DreamsOfInfiniteGlass.Data.Mathematical;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.Data.Helper {
	public static class FNodeExtensions {

		/// <summary>
		/// Sets the position, rotation, and scale of the given <paramref name="node"/> to that 
		/// of the provided <paramref name="transform"/>.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="transform"></param>
		public static void SetTransform(this FNode node, in SpriteTransform2D transform) {
			node.x = transform.position.x;
			node.y = transform.position.y;
			node.rotation = transform.rotation;
			node.scaleX = transform.scale.x;
			node.scaleY = transform.scale.y;
		}

		/// <summary>
		/// Reads the position, rotation, and scale from the provided <paramref name="node"/> 
		/// into a new <see cref="SpriteTransform2D"/>.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static SpriteTransform2D GetTransform(this FNode node) {
			return new SpriteTransform2D(
				new Vector2(node.x, node.y),
				node.rotation,
				new Vector2(node.scaleX, node.scaleY)
			);
		}

	}
}
