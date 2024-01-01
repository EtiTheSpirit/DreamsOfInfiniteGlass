#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities.Attributes;
using RWBodyChunkConnection = PhysicalObject.BodyChunkConnection;
using RWOracleArm = Oracle.OracleArm;
using Random = UnityEngine.Random;
using XansTools.Utilities;
using DreamsOfInfiniteGlass.Character.NPC.Iterator.Graphics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DreamsOfInfiniteGlass.Character.NPC.Iterator {
	public sealed class GlassOracle : Extensible.Oracle {

		public override bool Consious => health > 0;

		internal static void Initialize() {
			On.Oracle.ctor += (originalMethod, @this, abstractPhysicalObject, room) => {
				originalMethod(@this, abstractPhysicalObject, room);
				if (room.oracleWantToSpawn == Oracles.GlassID) {
					Binder<GlassOracle>.Bind(@this, abstractPhysicalObject, room);
				}
			};
			On.Oracle.Destroy += (originalMethod, @this) => {
				if (@this.ID == Oracles.GlassID) {
					Binder<GlassOracle>.TryReleaseBinding(@this);
				}
				originalMethod(@this);
			};
			On.Room.ReadyForAI += (originalMethod, @this) => {
				originalMethod(@this);
				if (@this.abstractRoom.name == DreamsOfInfiniteGlassPlugin.AI_CHAMBER) {
					if (@this.world != null && @this.game != null) {
						Log.LogTrace($"I want to spawn glass, the room is {DreamsOfInfiniteGlassPlugin.AI_CHAMBER}.");
						@this.oracleWantToSpawn = Oracles.GlassID;
						try {
							if (@this.abstractRoom == null) {
								Log.LogWarning("But I cannot, because the abstract room is null.");
								return;
							}
							Oracle obj = new Oracle(
								new AbstractPhysicalObject(@this.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(@this.abstractRoom.index, 15, 15, -1), @this.game.GetNewID()),
								@this
							);
							Log.LogTrace("Construction complete.");
							@this.AddObject(obj);
							@this.waitToEnterAfterFullyLoaded = Math.Max(@this.waitToEnterAfterFullyLoaded, 80);
						} catch (Exception exc) {
							Log.LogError($"Failed to spawn Glass: {exc}");
						}
					}
				}
			};
		}


		/// <summary>
		/// This save string is used when determining <see cref="HasTalkedBefore"/>
		/// </summary>
		private const string SAVE_KEY_HAS_TALKED_BEFORE = "TALKED_TO_GLASS";
		private bool? _cachedHasTalkedBefore = null;

		/// <summary>
		/// Its really funny (trust me bro)
		/// </summary>
		private bool _isDeadWithFunnyRagdoll = false;
		private int _funnyTicks = 0;

		/// <summary>
		/// Whether or not the player has talked to Glass.
		/// </summary>
		public bool HasTalkedBefore {
			get {
				if (_cachedHasTalkedBefore == null) {
					if (room == null) return false;
					if (room.game == null) return false;
					if (room.game.IsStorySession) {
						StoryGameSession story = (StoryGameSession)room.game.session;
						_cachedHasTalkedBefore = story.saveState.deathPersistentSaveData.unrecognizedSaveStrings.Contains(SAVE_KEY_HAS_TALKED_BEFORE);
						return _cachedHasTalkedBefore.Value;
					}
					_cachedHasTalkedBefore = false;
				}
				return _cachedHasTalkedBefore.Value;
			}
			set {
				if (value == HasTalkedBefore) return;
				if (room == null) return;
				if (room.game == null) return;
				if (room.game.IsStorySession) {
					StoryGameSession story = (StoryGameSession)room.game.session;
					List<string> data = story.saveState.deathPersistentSaveData.unrecognizedSaveStrings;
					if (value) {
						data.Add(SAVE_KEY_HAS_TALKED_BEFORE);
					} else {
						data.Remove(SAVE_KEY_HAS_TALKED_BEFORE);
					}
					_cachedHasTalkedBefore = value;
				}
			}
		}

#pragma warning disable IDE0051, IDE0060
		GlassOracle(Oracle original, AbstractPhysicalObject abstractPhysicalObject, Room inRoom) : base(original) {
			Log.LogDebug("Extensible.Oracle for Dreams of Infinite Glass constructed.");
			// Undo all of the garbage that the base ctor just did.

			ID = Oracles.GlassID;
			Log.LogTrace("Set ID.");
			health = 16161616; // 16 // 16 // 16 // 16 //
			Log.LogTrace("Set health.");
			ResetIterator();
			Log.LogTrace("Reset iterator.");
			CreateChunksAt(new Vector2(350f, 350f));
			Log.LogTrace("Created body chunks.");
			bodyChunkConnections = new RWBodyChunkConnection[] {
				new RWBodyChunkConnection(bodyChunks[0], bodyChunks[1], 9f, RWBodyChunkConnection.Type.Normal, 1f, 0.5f)
			};
			Log.LogTrace("Created body chunk connections.");
			oracleBehavior = new GlassOracleBehavior(this);
			Log.LogTrace("Created behavior.");
		}
#pragma warning restore IDE0051, IDE0060


		#region Utility Methods
		/// <summary>
		/// Resets all data stored in this object. Destroys all objects. A clean slate.
		/// </summary>
		private void ResetIterator() {
			mySwarmers?.DestroyAllAndClear();
			Log.LogTrace("Destroyed stray neurons.");
			marbles?.DestroyAllAndClear();
			Log.LogTrace("Destroyed stray pearls.");
			myScreen?.Destroy();
			myScreen = null;
			Log.LogTrace("Destroyed unused projection screen.");
			MoonLight?.Destroy();
			MoonLight = null;
			Log.LogTrace("Destroyed extra light source.");

			bodyChunks = new BodyChunk[2];
			Log.LogTrace("Created new body chunks.");
			airFriction = 0.99f;
			gravity = 0f;
			bounce = 0.1f;
			surfaceFriction = 0.17f;
			collisionLayer = 1;
			waterFriction = 0.92f;
			buoyancy = 0f;
			Log.LogTrace("Physical properties set.");
			arm = new RWOracleArm(this);
			Log.LogTrace("Constructed a new arm.");
			arm.isActive = true;
			Log.LogTrace("Activated this arm.");
			arm.cornerPositions[0] = room.MiddleOfTile(9, 32);
			arm.cornerPositions[1] = room.MiddleOfTile(37, 32);
			arm.cornerPositions[2] = room.MiddleOfTile(37, 4);
			arm.cornerPositions[3] = room.MiddleOfTile(9, 4);
			Log.LogTrace("Set arm corner positions.");
		}

		/// <summary>
		/// Creates two new <see cref="BodyChunk"/>s at the provided location for this iterator's body.
		/// </summary>
		/// <param name="position"></param>
		private void CreateChunksAt(Vector2 position) {
			bodyChunks[0] = new BodyChunk(this, 0, position, 6f, 0.5f);
			bodyChunks[1] = new BodyChunk(this, 1, position, 6f, 0.5f);
		}
		#endregion

		public override void Update(bool eu) {
			base.Update(eu);

			if (eu && _isDeadWithFunnyRagdoll) {
				_funnyTicks++;
				if (_funnyTicks >= 4) {
					bodyChunks[0].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					bodyChunks[1].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					_funnyTicks = 0;
				}
			}
		}

		public void FuckingDie(bool withFunnyRagdoll) {
			if (health == 0) return;
			// His last words were "who the hell is steve jobs?"

			Vector2 pos = bodyChunks[0].pos;
			room.AddObject(new ShockWave(pos, 500f, 0.75f, 18, false));
			room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));
			room.PlaySound(SoundID.Firecracker_Bang, pos, 1f, 0.75f + Random.value);
			room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 1f, 0.5f + Random.value * 0.5f);
			health = 0;
			gravity = 0.9f;
			_isDeadWithFunnyRagdoll = withFunnyRagdoll;
		}

	}
}
