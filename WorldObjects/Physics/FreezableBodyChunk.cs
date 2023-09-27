#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.WorldObjects.Physics {

	/// <summary>
	/// An extension upon <see cref="BodyChunk"/> that can be "anchored" or "frozen".
	/// While in this state, its velocity is always zero, and its position is stored + reset every Update().
	/// </summary>
	public sealed class FreezableBodyChunk : Extensible.BodyChunk {

		[Obsolete("To future Xan: Unlike other Extensible types, you are actually going to want to bind this one when you construct the corresponding BodyChunk.", true)]
		internal static void Initialize() { }

#pragma warning disable IDE0051, IDE0060
		FreezableBodyChunk(BodyChunk original) : base(original) { }
#pragma warning restore IDE0051, IDE0060

		/// <summary>
		/// If true, this <see cref="BodyChunk"/> is unable to move; its position will remain fixed in space and its velocity
		/// will always be zero.
		/// </summary>
		public bool Frozen {
			get => _frozenPos != null;
			set {
				if (value == Frozen) return;
				if (value) {
					_frozenPos = pos;
				} else {
					_frozenPos = null;
				}
			}
		}
		private Vector2? _frozenPos = null;

		public override void Update() {
			if (Frozen) {
				vel = Vector2.zero;
				HardSetPosition(_frozenPos!.Value);
			}
			base.Update();
		}
	}
}
