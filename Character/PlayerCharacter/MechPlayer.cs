#nullable enable
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Character.PlayerCharacter.DataStorage;
using DreamsOfInfiniteGlass.Character.PlayerCharacter.FX;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.RW;
using Random = UnityEngine.Random;
using DreamsOfInfiniteGlass.WorldObjects.Physics;
using XansTools.Utilities.General;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {

	/// <summary>
	/// This class represents SOLSTICE and all of its abilities.
	/// </summary>
	public sealed class MechPlayer : Extensible.Player {

		/// <summary>
		/// Damage in <see cref="Violence(BodyChunk, Vector2?, BodyChunk, PhysicalObject.Appendage.Pos, Creature.DamageType, float, float)"/> is multiplied by this value
		/// when using SOLSTICE's unique health system.
		/// </summary>
		public const float DAMAGE_SCALE = 9.0f;

		/// <summary>
		/// The player's battery. This is a replacement for their cycle timer.
		/// </summary>
		public MechPlayerBattery Battery { get; }

		/// <summary>
		/// The health of this player, in a more game-like mechanical sense.
		/// </summary>
		public MechPlayerHealth Health { get; }

		private static DisembodiedLoopEmitter? _deadIdleSound = null;
		private CollapseEffect? _currentCollapseEffect = null;
		private bool _hasAlreadyExplodedForDeath = false;

		private WeakReference<FreezableBodyChunk> _anchorable = new WeakReference<FreezableBodyChunk>(null!);

		/// <summary>
		/// A reference to <see cref="PhysicalObject.firstChunk"/> as a <see cref="FreezableBodyChunk"/>.
		/// </summary>
		public FreezableBodyChunk? FirstChunkAnchorable => _anchorable.Get();

		internal static void Initialize() {
			On.Player.ctor += (originalMethod, @this, abstractCreature, world) => {
				originalMethod(@this, abstractCreature, world);
				if (@this.slugcatStats.name == Slugcats.MechID) {
					Log.LogDebug("Custom character is constructing...");
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

#pragma warning disable IDE0051, IDE0060
		MechPlayer(Player original, AbstractCreature abstractCreature, World world) : base(original) {
			Log.LogDebug("Extensible.Player for MechPlayer constructed.");
			Battery = new MechPlayerBattery(original);
			Health = new MechPlayerHealth(original);

			// Now bind this.
			_anchorable = Extensible.BodyChunk.Binder<FreezableBodyChunk>.Bind(original.firstChunk);
		}
#pragma warning restore IDE0051, IDE0060

		public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType damageType, float damage, float stunBonus) {
			const float overrideDamage = 0f;
			base.Violence(source, directionAndMomentum, hitChunk, hitAppendage, damageType, overrideDamage, stunBonus);
			// Above: Call the original violence method but with 0 damage. This will prevent death under ordinary circumstances.

			// Now I can manually handle the real damage here for the mech player.
			// In general, Rain World entities will deal anywhere from 0 to 2 damage in most cases.
			float scaledDamage = damage * DAMAGE_SCALE;
			Log.LogDebug($"Mech got attacked. The source is {source} ({source?.owner}) with {damage} (=> {scaledDamage}) damage.");
			if (source.TryGetOwnerAs(out Weapon weapon)) {
				Health.TakeDamage(scaledDamage, weapon);
			} else if (source.TryGetOwnerAs(out Creature creature)) {
				Health.TakeDamage(scaledDamage, creature, damageType);
			} else {
				Health.TakeDamage(scaledDamage, source?.owner);
			}
			/*
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
			*/
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
			_ = base.AllowGrabbingBatflys();
			Log.LogTrace("Disallowing the player from grabbing a batfly.");
			return false;
		}

		public override bool CanEatMeat(Creature crit) {
			_ = base.CanEatMeat(crit);
			Log.LogTrace("Disallowing the player from eating meat.");
			return false;
		}

		public override void Deafen(int df) {
			Log.LogTrace("Deafen duration will be divided by 8.");
			base.Deafen(df >> 3);
		}

		public override float DeathByBiteMultiplier() {
			float _ = base.DeathByBiteMultiplier();
			Log.LogTrace("DeathByBiteMultiplier is being set to 0.");
			return 0;
		}



		public override void Update(bool eu) {
			base.Update(eu);

			// Manage stats
			airInLungs = 1.0f;
			drown = 0.0f;
			lungsExhausted = false;
			exhausted = false;
			
			if (room != null) {
				if (dead && !_hasAlreadyExplodedForDeath) {
					// _hasAlreadyExplodedForDeath is false when it's not the extreme death, so I can use this here too.
					_deadIdleSound ??= room.PlayDisembodiedLoop(Sounds.MECH_BROKEN_LOOP, 1.0f, 1.0f, 0.0f);
					_deadIdleSound.alive = true; // This must be set every frame.
				} else {
					if (_deadIdleSound != null) {
						_deadIdleSound.Destroy();
						_deadIdleSound = null;
					}
				}

				Battery.Update();
				if (_currentCollapseEffect != null && firstChunk != null) {
					_currentCollapseEffect.UpdatePosition(ref _currentCollapseEffect, firstChunk.pos);
				} else if (_currentCollapseEffect != null && _currentCollapseEffect.DetonationCompleted) {
					if (FirstChunkAnchorable != null) {
						FirstChunkAnchorable.Frozen = true;
					}
					Destroy();
				} else if (!slatedForDeletetion) {
					if (FirstChunkAnchorable != null) {
						FirstChunkAnchorable.Frozen = false;
					}
				}

				const float DISRUPTOR_RECHARGE_DIST = 400;
				const float DISRUPTOR_RECHARGE_DIST_SQR = DISRUPTOR_RECHARGE_DIST * DISRUPTOR_RECHARGE_DIST;
				if (firstChunk != null) {
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
				}

				if (!Battery.IsBatteryDead) {
					Battery.AlreadyHandledDeath = false;
				}
				if (Battery.IsBatteryDead && !Battery.AlreadyHandledDeath) {
					Battery.AlreadyHandledDeath = true;
					Die(); // L + Skill Issue
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
