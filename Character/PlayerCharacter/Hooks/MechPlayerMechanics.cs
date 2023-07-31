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
using XansCharacter.Data.Registry;
using XansTools.Utilities;

namespace XansCharacter.Character.PlayerCharacter.Hooks {

	/// <summary>
	/// Mechanical information about the player.
	/// </summary>
	public static partial class MechPlayerMechanics {

		private static readonly ConditionalWeakTable<Player, MechPlayerData> _runtimeData = new ConditionalWeakTable<Player, MechPlayerData>();

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
		/// <param name="creature"></param>
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
		public static MechPlayerData GetRuntimeData(Player @for) {
			if (!_runtimeData.TryGetValue(@for, out MechPlayerData data)) {
				data = new MechPlayerData(@for);
				_runtimeData.Add(@for, data);
			}
			return data;
		}

		internal static void Initialize() {
			Log.LogMessage("Initializing player...");


			On.Player.ctor += OnPlayerConstruction;

			On.Player.Update += OnUpdate;
		}



		private static void OnUpdate(On.Player.orig_Update originalMethod, Player @this, bool eu) {
			originalMethod(@this, eu);
			if (!@this.IsMechSlugcat()) return;
			MechPlayerData data = GetRuntimeData(@this);
			data.Update();

			const float DISRUPTOR_RECHARGE_DIST = 400;
			const float DISRUPTOR_RECHARGE_DIST_SQR = DISRUPTOR_RECHARGE_DIST * DISRUPTOR_RECHARGE_DIST;
			Vector2 playerPos = @this.firstChunk.pos;
			UpdatableAndDeletable[] disruptors = @this.room.updateList.Where(obj => obj is GravityDisruptor).ToArray(); // TODO: Is this even a good idea?
																												  // I could cache it.
			foreach (UpdatableAndDeletable obj in disruptors) {
				if (obj is GravityDisruptor grav) {
					if ((grav.pos - playerPos).sqrMagnitude < DISRUPTOR_RECHARGE_DIST_SQR) {
						data.BatteryCharge += Mathematical.RW_DELTA_TIME * 2f; // Allow overcharge?
					}
				}
			}

			if (data.BatteryCharge <= 0) {
				@this.Die(); // L
				return;
			}

			@this.airFriction = 0.5f;
			@this.bounce = 0f;
			@this.surfaceFriction = 0.8f;
			@this.waterFriction = 0.7f;
			@this.buoyancy = -0.5f;

		}

		


		private static void OnPlayerConstruction(On.Player.orig_ctor originalMethod, Player @this, AbstractCreature abstractCreature, World world) {
			originalMethod(@this, abstractCreature, world);
			_runtimeData.Add(@this, new MechPlayerData(@this));
		}
	}
}
