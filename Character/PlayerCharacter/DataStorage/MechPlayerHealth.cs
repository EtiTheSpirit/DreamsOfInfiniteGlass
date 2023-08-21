using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XansCharacter.Character.PlayerCharacter.DataStorage {

	/// <summary>
	/// Stats in a more traditional or "game-like" sense for the player's vitality.
	/// </summary>
	public sealed class MechPlayerHealth {

		public float Health { get; private set; } = 100f;

		public float MaxHealth { get; private set; } = 100f;

		/// <summary>
		/// Take damage that came from a specific creature.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		public void TakeDamage(float damage, Creature from, Creature.DamageType damageType = null) {
			
		}

		/// <summary>
		/// Take damage that came from a specific weapon.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		public void TakeDamage(float damage, Weapon from) {

		}

		/// <summary>
		/// Take damage that came from an object in the world.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		public void TakeDamage(float damage, PlacedObject from) {
			
		}

		/// <summary>
		/// Take damage from an arbitrary object, or from no source (environmental damage).
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		public void TakeDamage(float damage) {

		}

	}
}
