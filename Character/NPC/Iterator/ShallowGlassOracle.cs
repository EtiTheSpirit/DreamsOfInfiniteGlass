using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities.General;
using XansTools.Utilities;
using XansCharacter.Character.NPC.Iterator.Graphics;
using XansTools.PrimaryToolkit.CompanionData;
using XansCharacter.Data.Registry;
using Random = UnityEngine.Random;

namespace XansCharacter.Character.NPC.Iterator {
	public sealed class ShallowGlassOracle : MirrorOracle {

		public override Oracle Mirror => _baseContainer.Get();
		private readonly WeakReference<Oracle> _baseContainer = new WeakReference<Oracle>(null);

		/// <summary>
		/// Its really funny (trust me bro)
		/// </summary>
		private bool _isDeadWithFunnyRagdoll = false;
		private int _funnyTicks = 0;

		/// <summary>
		/// This save string is used when determining <see cref="HasTalkedBefore"/>
		/// </summary>
		private const string SAVE_KEY_HAS_TALKED_BEFORE = "TALKED_TO_GLASS";
		private bool? _cachedHasTalkedBefore = null;

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

		public bool IsConsious() {
			return health > 0;
		}

		public ShallowGlassOracle(Oracle original) {
			// Undo all of the garbage that the base ctor just did.
			_baseContainer.SetTarget(original);

			ID = Oracles.GlassID;
			health = 16161616; // 16 // 16 // 16 // 16 //
			ResetIterator();
			CreateChunksAt(new Vector2(500, 360));
			bodyChunkConnections = new PhysicalObject.BodyChunkConnection[] {
				new PhysicalObject.BodyChunkConnection(bodyChunks[0], bodyChunks[1], 9f, PhysicalObject.BodyChunkConnection.Type.Normal, 1f, 0.5f)
			};
			oracleBehavior = new GlassOracleBehavior(this);
		}
		
		#region Utility Methods
		/// <summary>
		/// Resets all data stored in this object. Destroys all objects. A clean slate.
		/// </summary>
		private void ResetIterator() {
			mySwarmers.RemoveThenDestroyAll();
			marbles.RemoveThenDestroyAll();
			myScreen.RemoveThenDestroy();
			MoonLight.RemoveThenDestroy();

			bodyChunks = new BodyChunk[2];
			airFriction = 0.99f;
			gravity = 0f;
			bounce = 0.1f;
			surfaceFriction = 0.17f;
			collisionLayer = 1;
			waterFriction = 0.92f;
			buoyancy = 0f;
			arm = new Oracle.OracleArm(this);
			arm.isActive = true;
			arm.cornerPositions[0] = room.MiddleOfTile(9, 32);
			arm.cornerPositions[1] = room.MiddleOfTile(37, 32);
			arm.cornerPositions[2] = room.MiddleOfTile(37, 4);
			arm.cornerPositions[3] = room.MiddleOfTile(9, 4);
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

		public void Update(bool eu) {
			if (eu && _isDeadWithFunnyRagdoll) {
				_funnyTicks++;
				if (_funnyTicks >= 4) {
					bodyChunks[0].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					bodyChunks[1].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					_funnyTicks = 0;
				}
			}
		}

		public void FuckingDie(bool funnyRagdoll) {
			if (health == 0) return;
			Vector2 pos = bodyChunks[0].pos;
			room.AddObject(new ShockWave(pos, 500f, 0.75f, 18, false));
			room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 5, Color.white));
			room.PlaySound(SoundID.Firecracker_Bang, pos, 1f, 0.75f + Random.value);
			room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, pos, 1f, 0.5f + Random.value * 0.5f);
			health = 0;
			gravity = 0.9f;
			_isDeadWithFunnyRagdoll = funnyRagdoll;
		}

		public static implicit operator Oracle(ShallowGlassOracle @this) => @this.Mirror;
	}
}
