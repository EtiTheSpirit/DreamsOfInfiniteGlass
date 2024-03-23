using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter.Interactions {
	public static class Spears {

		internal static void Initialize() {
			On.Spear.Update += OnSpearUpdated;
			On.Spear.ChangeMode += OnSpearChangingMode;
		}

		private static void OnSpearChangingMode(On.Spear.orig_ChangeMode originalMethod, Spear @this, Weapon.Mode newMode) {
			originalMethod(@this, newMode);
			if (@this.thrownBy is Player player && MechPlayer.From(player) is MechPlayer solstice) {
				if ((newMode == Weapon.Mode.StuckInWall || newMode == Weapon.Mode.StuckInCreature) && @this is ExplosiveSpear explosive) {
					explosive.Explode();
				}
			}
		}

		private static void OnSpearUpdated(On.Spear.orig_Update originalMethod, Spear @this, bool eu) {
			originalMethod(@this, eu);
			if (@this.thrownBy is Player player && MechPlayer.From(player) is MechPlayer solstice) {
				if (@this.mode == Weapon.Mode.Thrown) {
					// @this.firstChunk.vel.y -= 0.45f; // Undo the +0.45f
					@this.waterRetardationImmunity = 1f;
				}
			}
		}
	}
}
