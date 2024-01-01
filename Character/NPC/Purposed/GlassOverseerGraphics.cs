#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.Character.NPC.Purposed {

	/// <summary>
	/// This class handles the graphics for overseers owned by Glass
	/// </summary>
	public sealed class GlassOverseerGraphics : Extensible.OverseerGraphics {

		/// <summary>
		/// 16 // 16 // 16 // 16
		/// </summary>
		public const int GLASS_OVERSEER_IDENTITY = 16161616;

		/// <summary>
		/// The color of Glass's overseer.
		/// </summary>
		public static Color GLASS_OVERSEER_COLOR => new Color(0.8089771f, 1.0f, 0.3962264f);

		/// <summary>
		/// If true, the system always spawns overseers as that belonging to this iterator.
		/// </summary>
		public const bool ALWAYS_SPAWN_AS_GLASS_OVERSEER = false;

		public override Color MainColor {
			get {
				Color original = base.MainColor;
				if (overseer.abstractCreature.abstractAI is OverseerAbstractAI ai) {
					if (ai.ownerIterator == GLASS_OVERSEER_IDENTITY || ALWAYS_SPAWN_AS_GLASS_OVERSEER) {
						return GLASS_OVERSEER_COLOR;
					}
				}
				return original;
			}
		}

		internal static void Initialize() {
			On.OverseerGraphics.ctor += (originalMethod, @this, physObj) => {
				originalMethod(@this, physObj);
				if (@this.owner is Overseer overseer && overseer.abstractCreature.abstractAI is OverseerAbstractAI ai) {
					Binder<GlassOverseerGraphics>.Bind(@this);
				}
			};
			On.OverseerAbstractAI.ctor += OnConstructingOverseerAI;
			// ownerIterator
		}

		private static void OnConstructingOverseerAI(On.OverseerAbstractAI.orig_ctor originalMethod, OverseerAbstractAI @this, World world, AbstractCreature parent) {
			originalMethod(@this, world, parent);
			if (!world.singleRoomWorld) {
				if (world.region.name == "16") {
					@this.parent.ignoreCycle = true;
					Log.LogDebug("Setting overseer to ignore the cycle while within Glass.");

					// TODO: Other iterator colors?
					@this.ownerIterator = GLASS_OVERSEER_IDENTITY;
				} else {
					if ((UnityEngine.Random.value < 0.001 && world.region.name != "HR") || ALWAYS_SPAWN_AS_GLASS_OVERSEER) {
						// Allow glass's overseers to appear anywhere (other than Rubicon) in any timeline.
						@this.ownerIterator = GLASS_OVERSEER_IDENTITY;
					}
				}
			}
		}
		GlassOverseerGraphics(OverseerGraphics original) : base(original) { }

	}
}
