using HUD;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.Character.PlayerCharacter.FX;
using XansCharacter.Data.Players;
using XansCharacter.Data.Registry;
using XansTools.Utilities;

namespace XansCharacter.Character.PlayerCharacter {

	/// <summary>
	/// Mechanical information about the player.
	/// </summary>
	[Obsolete]
	public static class MechPlayerMechanics {

		private static readonly ConditionalWeakTable<Player, MechRuntimeData> _runtimeData = new ConditionalWeakTable<Player, MechRuntimeData>();

		/// <summary>
		/// Show a message to the player at the bottom of the screen. This is an interruption.
		/// </summary>
		/// <param name="to"></param>
		/// <param name="message"></param>
		public static void ShowMessage(Player to, string message) {
			to.room.game.cameras[0].hud.textPrompt.AddMessage(message, 240, 480, true, true);
		}

		/// <summary>
		/// Returns whether or not the provided player is the custom character.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static bool IsMechSlugcat(this Creature creature) {
			if (creature is Player player) {
				return player.SlugCatClass == Slugcats.MechID;
			}
			return false;
		}

		/// <summary>
		/// Attempts to get ahold of the runtime data for the player. Creates it if it does not exist.
		/// </summary>
		/// <param name="for"></param>
		/// <returns></returns>
		public static MechRuntimeData GetRuntimeData(Player @for) {
			if (!_runtimeData.TryGetValue(@for, out MechRuntimeData data)) {
				data = new MechRuntimeData();
				_runtimeData.Add(@for, data);
			}
			return data;
		}

		internal static void Initialize() {
			Log.LogMessage("Initializing player...");
			On.Player.ctor += OnPlayerConstruction;
			On.Player.AllowGrabbingBatflys += OnAllowGrabbingBatflys;
			// TODO: What can this character swallow?
			On.Player.CanEatMeat += OnCanEatMeat;
			// CanIPickThisUp
			// CanIPutDeadSlugOnBack
			// CanMaulCreature
			// checkInput
			On.Player.Deafen += OnDeafen;
			On.Player.DeathByBiteMultiplier += OnGetDeathByBiteMultiplier;
			On.Player.Update += OnUpdate;
			On.Creature.SpearStick += OnCanSpearStick;
			On.Creature.Violence += OnViolenceCommitted;

			// TODO: Can I inject into the finalizer?

			// TODO: Grab methods
			// TODO: Jumping
			// 

			On.Player.Die += OnDie;
		}

		private static void OnViolenceCommitted(On.Creature.orig_Violence originalMethod, Creature @this, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus) {
			originalMethod(@this, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);

		}

		private static bool OnCanSpearStick(On.Creature.orig_SpearStick originalMethod, Creature @this, Weapon source, float dmg, BodyChunk chunk, PhysicalObject.Appendage.Pos appPos, Vector2 velocity) {
			bool wouldStick = originalMethod(@this, source, dmg, chunk, appPos, velocity);
			if (IsMechSlugcat(@this)) {
				if (wouldStick) {
					// TODO: Anything special here?
				}
			}
			return wouldStick;
		}

		private static void OnDie(On.Player.orig_Die originalMethod, Player @this) {
			if (IsMechSlugcat(@this)) {
				MechRuntimeData data = GetRuntimeData(@this);
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
			} else {
				originalMethod(@this);
			}
		}

		private static void OnUpdate(On.Player.orig_Update originalMethod, Player @this, bool eu) {
			originalMethod(@this, eu);
			GetRuntimeData(@this).Update();
		}

		private static float OnGetDeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier originalMethod, Player @this) {
			float desired = originalMethod(@this);
			if (IsMechSlugcat(@this)) {
				return Mathf.Min(desired, 0.07f);
			}
			return desired;
		}

		private static void OnDeafen(On.Player.orig_Deafen originalMethod, Player @this, int df) {
			if (IsMechSlugcat(@this)) {
				df = 0;
			}
			originalMethod(@this, df);
		}

		private static bool OnCanEatMeat(On.Player.orig_CanEatMeat originalMethod, Player @this, Creature crit) {
			if (IsMechSlugcat(@this)) {
				return false;
			}
			return originalMethod(@this, crit);
		}

		private static bool OnAllowGrabbingBatflys(On.Player.orig_AllowGrabbingBatflys originalMethod, Player @this) {
			if (IsMechSlugcat(@this)) {
				return false;
			}
			return originalMethod(@this);
		}

		private static void OnPlayerConstruction(On.Player.orig_ctor originalMethod, Player @this, AbstractCreature abstractCreature, World world) {
			originalMethod(@this, abstractCreature, world);
			_runtimeData.Add(@this, new MechRuntimeData());
		}
	}
}
