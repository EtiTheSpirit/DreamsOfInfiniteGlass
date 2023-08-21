using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.Character.PlayerCharacter.FX;
using XansCharacter.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.RW;
using Random = UnityEngine.Random;

namespace XansCharacter.Character.PlayerCharacter {
	public sealed class MechPlayer : Extensible.Player {

		private CollapseEffect _currentCollapseEffect = null;
		
		private bool _hasAlreadyExplodedForDeath;

		public MechPlayerBattery Battery { get; }

		/// <summary>
		/// The health of this player, in a more game-like mechanical sense.
		/// </summary>
		public MechPlayerHealth Health { get; }

		internal static void Initialize() {
			On.Player.ctor += (originalMethod, @this, abstractCreature, world) => {
				originalMethod(@this, abstractCreature, world);
				if (@this.slugcatStats.name == Slugcats.MechID) {
					WeakReference<MechPlayer> mech = Binder<MechPlayer>.Bind(@this, abstractCreature, world);
				}
			};
			On.Player.Destroy += (originalMethod, @this) => {
				originalMethod(@this);
				if (@this.slugcatStats.name == Slugcats.MechID) {
					Binder<MechPlayer>.TryReleaseBinding(@this);
				}
			};
		}

		private MechPlayer() : base(null) { }

		MechPlayer(Player original, AbstractCreature abstractCreature, World world) : base(original) {
			Log.LogDebug("Extensible.Player for MechPlayer constructed.");
			Battery = new MechPlayerBattery();
			Health = new MechPlayerHealth();
		}

		public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus) {
			const float overrideDamage = 0f;
			base.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, overrideDamage, stunBonus);
			// Above: Call the original violence method but with 0 damage. This will prevent death under ordinary circumstances.

			// Now I can manually handle the real damage here for the mech player.
			Log.LogDebug($"Mech got attacked. The source is {source} ({source?.owner}) with {damage} damage.");
			if (source.TryGetOwnerAs(out Weapon weapon)) {
				if (weapon is ElectricSpear electricSpear) {
					if (electricSpear.stuckInObject == (Player)this) {
						Die(true);
					} else {
						// It didn't get stuck but was still a big hit.
						// TODO: What are the damage values that are usually seen?
						Battery.ClampedCharge -= (Random.value * 4f) + 2f;
					}
				} else if (weapon is ExplosiveSpear explosiveSpear) {
					// TODO
				} else {
					// TODO
				}
			} else if (source.TryGetOwnerAs(out Creature creature)) {
				// TODO
			} else {
				// TODO
				if (type == Creature.DamageType.Electric) {
					
				} else if (type == Creature.DamageType.Explosion) {
					
				}
			}
		}


		public override void Die() => Die(false);

		/// <summary>
		/// Kill this player.
		/// </summary>
		/// <param name="causeCollapse">If true, the player will violently detonate as their reactor core goes critical. If false, they just die normally.</param>
		public void Die(bool causeCollapse) {
			if (dead) return;
			base.Die();
			Log.LogDebug($"Mech has died. Collapse: {causeCollapse}");

			// Check dead in case another mod canceled death in an unconventional way.
			if (!dead) {
				Log.LogTrace("Though, the player seems to have been revived or death was otherwise prevented. Nevermind.");
				_hasAlreadyExplodedForDeath = false; // Reset this
				return;
			}

			if (_hasAlreadyExplodedForDeath || !causeCollapse) return;
			Log.LogTrace("oof ouch owie my skin *cutely explodes so violently i rip apart reality for a sec*");
			_hasAlreadyExplodedForDeath = true;
			_currentCollapseEffect = new CollapseEffect(this);
			room.AddObject(_currentCollapseEffect);
		}

		public override bool AllowGrabbingBatflys() {
			return false;
		}

		public override bool CanEatMeat(Creature crit) {
			return false;
		}

		public override void Deafen(int df) {
			base.Deafen(df >> 2);
		}

		public override float DeathByBiteMultiplier() {
			return Mathf.Min(base.DeathByBiteMultiplier(), 0.07f); // TODO: Maybe 0 if I use a health system.
		}

		public override void Update(bool eu) {
			base.Update(eu);
			if (room != null) {
				Battery.Update(eu);
				if (_currentCollapseEffect != null && firstChunk != null) {
					_currentCollapseEffect.UpdatePosition(ref _currentCollapseEffect, firstChunk.pos);
				} else if (_currentCollapseEffect != null && _currentCollapseEffect.DetonationCompleted) {
					Destroy();
				}

				const float DISRUPTOR_RECHARGE_DIST = 400;
				const float DISRUPTOR_RECHARGE_DIST_SQR = DISRUPTOR_RECHARGE_DIST * DISRUPTOR_RECHARGE_DIST;
				Vector2 playerPos = firstChunk.pos;
				IEnumerable<UpdatableAndDeletable> disruptors = room.updateList.Where(obj => obj is GravityDisruptor);  // TODO: Is this even a good idea?
																														// I could cache it.
				foreach (UpdatableAndDeletable obj in disruptors) {
					if (obj is GravityDisruptor grav) {
						if ((grav.pos - playerPos).sqrMagnitude < DISRUPTOR_RECHARGE_DIST_SQR) {
							//BatteryCharge += Mathematical.RW_DELTA_TIME * 2f; // Allow overcharge?
							// TODO: Charge tokens
						}
					}
				}

				if (!Battery.IsDead) {
					Battery.AlreadyHandledDeath = false;
				}
				if (Battery.IsDead && !Battery.AlreadyHandledDeath) {
					Battery.AlreadyHandledDeath = true;
					Die(); // L
					return;
				}

				airFriction = 0.5f;
				bounce = 0f;
				surfaceFriction = 0.8f;
				waterFriction = 0.7f;
				buoyancy = -0.5f;
			}
		}
	}
}
