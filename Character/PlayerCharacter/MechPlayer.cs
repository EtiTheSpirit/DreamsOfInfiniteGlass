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
using DreamsOfInfiniteGlass.WorldObjects.Physics;
using XansTools.Utilities.General;
using System.Runtime.CompilerServices;
using XansTools.Utilities.RW.DataPersistence;
using DreamsOfInfiniteGlass.Data.Helper;
using static UnityEngine.UI.Image;

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

		/// <summary>
		/// The scale of gravity while underwater.
		/// </summary>
		public float UnderwaterGravity { get; set; } = 0.4f;

		private static DisembodiedLoopEmitter? _deadIdleSound = null;
		private CollapseEffect? _currentCollapseEffect = null;
		private bool _hasAlreadyExplodedForDeath = false;

		private WeakReference<SpecialBodyChunk> _anchorableFirst = new WeakReference<SpecialBodyChunk>(null!);
		private WeakReference<SpecialBodyChunk> _anchorableSecond = new WeakReference<SpecialBodyChunk>(null!);

		/// <summary>
		/// A reference to <see cref="PhysicalObject.firstChunk"/> as a <see cref="SpecialBodyChunk"/>.
		/// </summary>
		public SpecialBodyChunk? FirstChunkAnchorable => _anchorableFirst.Get();

		public SpecialBodyChunk? SecondChunkAnchorable => _anchorableSecond.Get();

		/// <summary>
		/// The mech's save data.
		/// </summary>
		public SaveDataAccessor SaveData { get; } = SaveDataAccessor.Get(DreamsOfInfiniteGlassPlugin.PLUGIN_ID, "MechPlayer");

		/// <summary>
		/// The average velocity of all body parts.
		/// </summary>
		public Vector2 AverageVelocity {
			get {
				float x = 0;
				float y = 0;
				float len = bodyChunks.Length;
				if (len == 0) return Vector2.zero;

				float iLen = 1.0f / len;
				for (int i = 0; i < len; i++) {
					BodyChunk chunk = bodyChunks[i];
					x += chunk.vel.x;
					y += chunk.vel.y;
				}

				return new Vector2(x * iLen, y * iLen);
			}
			set {
				float len = bodyChunks.Length;
				for (int i = 0; i < len; i++) {
					BodyChunk chunk = bodyChunks[i];
					chunk.vel = value;
				}
			}
		}

		/// <summary>
		/// If the provided player has a binding to <see cref="MechPlayer"/>, this returns the instance. Returns null otherwise.
		/// </summary>
		/// <param name="player"></param>
		/// <returns></returns>
		public static MechPlayer? From(Player player) => Binder<MechPlayer>.TryGetBinding(player, out MechPlayer mech) ? mech : null;
		
		internal static void Initialize() {
			On.Player.ctor += (originalMethod, @this, abstractCreature, world) => {
				originalMethod(@this, abstractCreature, world);
				if (@this.slugcatStats.name == Slugcats.MechID) {
					Log.LogDebug("Custom character is constructing...");
					Binder<MechPlayer>.Bind(@this, abstractCreature, world);
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
			_anchorableFirst = Extensible.BodyChunk.Binder<SpecialBodyChunk>.Bind(original.bodyChunks[0]);
			_anchorableSecond = Extensible.BodyChunk.Binder<SpecialBodyChunk>.Bind(original.bodyChunks[1]);
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

		public override void NewRoom(Room newRoom) {
			base.NewRoom(newRoom);

			if (FirstChunkAnchorable != null) {
				Extensible.BodyChunk.Binder<SpecialBodyChunk>.TryReleaseBinding(FirstChunkAnchorable);
			}
			if (SecondChunkAnchorable != null) {
				Extensible.BodyChunk.Binder<SpecialBodyChunk>.TryReleaseBinding(SecondChunkAnchorable);
			}

			_anchorableFirst = Extensible.BodyChunk.Binder<SpecialBodyChunk>.Bind(bodyChunks[0]);
			_anchorableSecond = Extensible.BodyChunk.Binder<SpecialBodyChunk>.Bind(bodyChunks[1]);
		}

		public void AboutToDie(System.Type sourceType, object? sourceInstance) {
			Log.LogMessage($"The mech is about to die, via a call from {sourceType.FullName} (instance: {sourceInstance})");
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

			if (!causeCollapse) {
				room.PlaySound(Sounds.MECH_DIE, firstChunk.pos, 0.5f, 1f);
				return;
			}

			if (_hasAlreadyExplodedForDeath) return;
			if (_currentCollapseEffect != null && !_currentCollapseEffect.DetonationCompleted) {
				Log.LogWarning("There is already a collapse effect defined, and that effect has not been completed! Skipping creation.");
				return;
			}

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
			_ = base.DeathByBiteMultiplier();
			Log.LogTrace("DeathByBiteMultiplier is being set to 0.");
			return 0;
		}

		public bool WouldBeSwimming() {
			if (FirstChunkAnchorable == null || SecondChunkAnchorable == null) {
				return bodyChunks[0].submersion >= 0.2f || bodyChunks[1].submersion >= 0.2f;
			}
			bool firstChunkDisabledSubmersion = FirstChunkAnchorable.DisallowSubmersion;
			bool secondChunkDisabledSubmersion = SecondChunkAnchorable.DisallowSubmersion;
			FirstChunkAnchorable.DisallowSubmersion = false;
			SecondChunkAnchorable.DisallowSubmersion = false;
			bool countsAsSwimming = bodyChunks[0].submersion >= 0.2f || bodyChunks[1].submersion >= 0.2f;
			FirstChunkAnchorable.DisallowSubmersion = firstChunkDisabledSubmersion;
			SecondChunkAnchorable.DisallowSubmersion = secondChunkDisabledSubmersion;
			return countsAsSwimming;
		}

		public override void MovementUpdate(bool eu) {
			if (FirstChunkAnchorable == null || SecondChunkAnchorable == null) {
				base.MovementUpdate(eu);
				return;
			}

			FirstChunkAnchorable.DisallowSubmersion = true;
			SecondChunkAnchorable.DisallowSubmersion = true;
			base.MovementUpdate(eu);
			FirstChunkAnchorable.DisallowSubmersion = false;
			SecondChunkAnchorable.DisallowSubmersion = false;
		}

		private void ApplyManualWaterDrag() {
			float len = bodyChunks.Length;
			for (int i = 0; i < len; i++) {
				BodyChunk chunk = bodyChunks[i];
				chunk.vel.x *= 0.97f;
				ref float y = ref chunk.vel.y;
				if (y > 0) {
					// going up, don't apply so much drag (this helps with the jump height)
					y *= 0.995f;
					y -= 0.1f;
				} else {
					y *= 0.95f;
					y -= 1f;
				}
			}
		}		

		public override void Update(bool eu) {
			base.Update(eu);

			if (WouldBeSwimming()) {
				// Do a manual velocity reduction.
				ApplyManualWaterDrag();
			}

			// Manage stats
			airInLungs = 1.0f;
			drown = 0.0f;
			aerobicLevel = 0.0f;
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
					Die(false); // L + Skill Issue
					return;
				}


				airFriction = 0.5f;
				bounce = 0f;
				surfaceFriction = 0.8f;
				waterFriction = 1f;
				buoyancy = 0f;
			}
		}

	}
}
