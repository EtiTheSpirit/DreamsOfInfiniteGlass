using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.Character.PlayerCharacter.FX;

namespace XansCharacter.Character.PlayerCharacter.Hooks {
	public static partial class MechPlayerMechanics {

		private static float DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier originalMethod, Player @this) {
			float desired = originalMethod(@this);
			if (IsMechSlugcat(@this)) {
				return Mathf.Min(desired, 0.07f);
			}
			return desired;
		}

		private static void Deafen(On.Player.orig_Deafen originalMethod, Player @this, int df) {
			if (IsMechSlugcat(@this)) {
				df >>= 2;
				if (df < 40) df = 0;
			}
			originalMethod(@this, df);
		}

		private static void Die(On.Player.orig_Die originalMethod, Player @this) {
			MechPlayerData data = GetRuntimeData(@this);
			if (data.HasAlreadyExplodedForDeath) return;
			if (@this.dead) return; // omae wa shineru (so don't run the death code again)

			originalMethod(@this);

			if (!@this.dead) {
				Log.LogTrace("Player seems to have been revived.");
				data.HasAlreadyExplodedForDeath = false; // Reset this
				return;
			}
			// Check dead in case another mod canceled death.


			Log.LogTrace("oof ouch owie my skin *cutely explodes so violently i rip apart reality for a sec*");
			data.HasAlreadyExplodedForDeath = true;
			@this.room.AddObject(new CollapseEffect(@this));
		}

	}
}
