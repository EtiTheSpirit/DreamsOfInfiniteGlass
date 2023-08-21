using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities;

namespace XansCharacter.Character.PlayerCharacter {
	public sealed class MechPlayerBattery {

		/// <summary>
		/// Charge, as a value from 0 to 100. The getter and setter will never return a value outside of this range.
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
		/// The change in charge per second. Negative values subtract charge, positive values add it.
		/// </summary>
		public float ChargeDeltaPerSecond { get; set; } = -0.02f;

		/// <summary>
		/// True if the battery is charging (<see cref="ChargeDeltaPerSecond"/> is greater than 0).
		/// </summary>
		public bool IsCharging => ChargeDeltaPerSecond > 0;

		/// <summary>
		/// True if the battery is draining (<see cref="ChargeDeltaPerSecond"/> is less than 0).
		/// </summary>
		public bool IsDraining => ChargeDeltaPerSecond < 0;

		/// <summary>
		/// True if the battery's <see cref="ClampedCharge"/> is equal to 0.
		/// </summary>
		public bool IsDead => ClampedCharge == 0;

		/// <summary>
		/// Whether or not the mech player already handled death.
		/// </summary>
		public bool AlreadyHandledDeath { get; set; } = false;

		/// <summary>
		/// Update the charge.
		/// </summary>
		/// <param name="eu"></param>
		public void Update(bool eu) {
			ClampedCharge += ChargeDeltaPerSecond * Mathematical.RW_DELTA_TIME;
		}
	}
}
