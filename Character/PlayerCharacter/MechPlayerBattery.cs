#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities;
using XansTools.Utilities.General;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public sealed class MechPlayerBattery {

		/// <summary>
		/// A reference to the original player object.
		/// </summary>
		public Player Player { get; }

		/// <summary>
		/// Attempts to get the <see cref="MechPlayer"/> associated with this object's <see cref="Player"/>. Returns null if the object was destroyed.
		/// </summary>
		public MechPlayer? PlayerAsMech => MechPlayer.From(Player);

		/// <summary>
		/// Charge, as a value from 0 to 100. The getter will never return a value outside of this range, 
		/// and the setter will clamp it to this range.
		/// </summary>
		public float ClampedCharge {
			get => Mathf.Clamp(_charge, 0f, 100f);
			set => _charge = Mathf.Clamp(value, 0f, 100f);
		}
		private float _charge = 100;

		/// <summary>
		/// Charge, as a value from 0 to 100. Values greater than 100 are permitted but values less than 0 are not.
		/// </summary>
		public float UnclampedCharge {
			get => Mathf.Max(_charge, 0);
			set => _charge = Mathf.Max(value, 0);
		}

		/// <summary>
		/// The change in value to the player's battery charge, per second. Negative values subtract charge, positive values add it.
		/// </summary>
		public float ChargeDeltaPerSecond { get; set; } = -0.02f;

		/// <summary>
		/// If true, swimming has an additional battery cost.
		/// </summary>
		/// <remarks>
		/// Depending on whether or not I add upgrades, this may or may not actually be used.
		/// </remarks>
		public bool ApplySwimPenalty { get; set; } = true;

		/// <summary>
		/// Swimming will incur this much charge drain per second (unlike <see cref="ChargeDeltaPerSecond"/>, positive values <em>increase</em> the drain).
		/// </summary>
		public float SwimChargeCost { get; set; } = 0.01f;

		/// <summary>
		/// True if the battery is charging (<see cref="EffectiveChargeDeltaPerSecond"/> is greater than 0).
		/// </summary>
		public bool IsCharging => EffectiveChargeDeltaPerSecond > 0;

		/// <summary>
		/// True if the battery is draining (<see cref="EffectiveChargeDeltaPerSecond"/> is less than 0).
		/// </summary>
		public bool IsDraining => EffectiveChargeDeltaPerSecond < 0;

		/// <summary>
		/// True if the battery's <see cref="ClampedCharge"/> is equal to 0.
		/// </summary>
		public bool IsBatteryDead => ClampedCharge == 0;

		/// <summary>
		/// Whether or not the mech player already handled death, as the out-of-charge
		/// death effect is in the Update() loop.
		/// </summary>
		public bool AlreadyHandledDeath { get; set; } = false;

		/// <summary>
		/// The effective charge delta per second.
		/// This is computed when referenced, as it relies on <see cref="Player.submerged"/>.
		/// </summary>
		public float EffectiveChargeDeltaPerSecond => ((Player.submerged && ApplySwimPenalty) ? -SwimChargeCost : 0) + ChargeDeltaPerSecond;

		public MechPlayerBattery(Player player) {
			Player = player;
		}

		/// <summary>
		/// Update the charge.
		/// </summary>
		/// <param name="eu">True if the update is on an even frame, false if not.</param>
		public void Update() {
			ClampedCharge += EffectiveChargeDeltaPerSecond * Mathematical.RW_DELTA_TIME;
		}
	}
}
