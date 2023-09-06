using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities.General;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter.DataStorage {

	/// <summary>
	/// Stats in a more traditional or "game-like" sense for the player's vitality.
	/// </summary>
	public sealed class MechPlayerHealth {

		/// <summary>
		/// The player's current health.
		/// In general, Rain World entities will deal anywhere from 0 to 2 damage in most cases.
		/// </summary>
		public float Health { get; private set; } = 100f;

		public float MaxHealth { get; private set; } = 100f;

		/// <summary>
		/// A reference to the original player object.
		/// </summary>
		public Player Player { get; }

		/// <summary>
		/// Attempts to get the <see cref="MechPlayer"/> associated with this object's <see cref="Player"/>. Returns <see langword="null"/> if the object was destroyed.
		/// </summary>
		public MechPlayer PlayerAsMech => Extensible.Player.Binder<MechPlayer>.TryGetBinding(Player, out WeakReference<MechPlayer> mech) ? mech.Get() : null;

		public MechPlayerHealth(Player player) {
			Player = player;
		}

		private void DoActualDamage(float damage, bool causesViolentDeath) {
			float newHealth = Health - damage;
			bool dead = false;
			if (newHealth <= 0) {
				dead = true;
				newHealth = 0;
			}
			Health = newHealth;

			if (dead) {
				if (PlayerAsMech != null) {
					PlayerAsMech.Die(causesViolentDeath);
					if (!causesViolentDeath) {
						
					}
				} else {
					throw new InvalidOperationException("An illegal state was encountered; something is still managing the mech player object, but is doing so after it has been destroyed. Something has gotten out of sync.");
				}
			} else {
				
			}
		}

		/// <summary>
		/// Take damage that came from a specific creature. This should only be called from Violence.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		internal void TakeDamage(float damage, Creature from, Creature.DamageType damageType = null) {
			bool violentDeath = false;
			if (damageType == Creature.DamageType.Electric) {
				damage *= 3.0f;
				violentDeath = true;
			} else if (damageType == Creature.DamageType.Water) {
				damage *= 2.0f;
			} else if (damageType == Creature.DamageType.Explosion) {
				damage *= 2.5f;
				violentDeath = UnityEngine.Random.value < 0.5f;
			}

			DoActualDamage(damage, violentDeath);
		}

		/// <summary>
		/// Take damage that came from a specific weapon. This should only be called from Violence.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from"></param>
		/// <param name="damageType">The damage type, or null for no type in particular.</param>
		internal void TakeDamage(float damage, Weapon from) {
			
		}

		/// <summary>
		// Take damage from an arbitrary object, or from no source (environmental damage). This should only be called from Violence.
		/// </summary>
		/// <param name="damage"></param>
		/// <param name="from">The object responsible, where applicable.</param>
		internal void TakeDamage(float damage, PhysicalObject from = null) {
			
		}

	}
}
