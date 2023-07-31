using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XansCharacter.Character.PlayerCharacter.Hooks {

	// Combat hooks
	public static partial class MechPlayerMechanics {

		private static void Violence(On.Creature.orig_Violence originalMethod, Creature @this, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus) {
			originalMethod(@this, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
			if (!@this.IsMechSlugcat()) return;
		}

		private static bool SpearStick(On.Creature.orig_SpearStick originalMethod, Creature @this, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 velocity) {
			bool wouldStick = originalMethod(@this, source, dmg, chunk, appPos, velocity);
			if (IsMechSlugcat(@this)) {
				if (wouldStick) {
					// TODO: Anything special here?
				}
			}
			return wouldStick;
		}
	}
}
