#line 1 "Character\\NPC\\Iterator\\GlassOracle.cs"
using HarmonyLib;
using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.NPC.Iterator.Graphics;
using XansCharacter.Character.NPC.Iterator.Interaction;
using XansCharacter.Data.Registry;
using XansCharacter.Utilities;
using Random = UnityEngine.Random;

namespace XansCharacter.Character.NPC.Iterator {
	public class GlassOracle : Oracle {

		/// <summary>
		/// Whether or not the iterator is conscious. Via a harmony patch, this effectively overrides that of <see cref="Oracle"/>
		/// despite shadowing it in C# (that is, calling it on Oracle will redirect here if it is an instance of <see cref="GlassOracle"/>).
		/// </summary>
		public new bool Consious => health > 0;

		private const string SAVE_KEY_HAS_TALKED_BEFORE = "TALKED_TO_GLASS";
		private bool? _cachedHasTalkedBefore = null;

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

		public GlassOracle(AbstractPhysicalObject abstractPhysicalObject, Room room, Vector2 position) : base(abstractPhysicalObject, room) {
			// Undo all of the garbage that the base ctor just did.
			ID = Oracles.GLASS_ORACLE_ID;
			health = 16161616;
			ResetIterator();
			CreateChunksAt(position);
			bodyChunkConnections = new BodyChunkConnection[] {
				new BodyChunkConnection(bodyChunks[0], bodyChunks[1], 9f, BodyChunkConnection.Type.Normal, 1f, 0.5f)
			};
			oracleBehavior = new GlassOracleBehavior(this);
		}




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
			arm = new OracleArm(this);
			arm.isActive = true;
			arm.cornerPositions[0] = room.MiddleOfTile(9, 32);
			arm.cornerPositions[1] = room.MiddleOfTile(37, 32);
			arm.cornerPositions[2] = room.MiddleOfTile(37, 4);
			arm.cornerPositions[3] = room.MiddleOfTile(9, 4);
		}

		public override void InitiateGraphicsModule() {
			if (graphicsModule == null) {
				graphicsModule = new GlassOracleGraphics(this);
			}
		}
		/// <summary>
		/// Creates two new <see cref="BodyChunk"/>s at the provided location for this iterator's body.
		/// </summary>
		/// <param name="position"></param>
		private void CreateChunksAt(Vector2 position) {
			bodyChunks[0] = new BodyChunk(this, 0, position, 6f, 0.5f);
			bodyChunks[1] = new BodyChunk(this, 1, position, 6f, 0.5f);
		}



		public override void Update(bool eu) {
			ERROR_TRACK_BLOCK_START
			base.Update(eu);
			if (eu && _isDeadWithFunnyRagdoll) {
				_funnyTicks++;
				if (_funnyTicks >= 10) {
					bodyChunks[0].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					bodyChunks[1].vel = new Vector2((Random.value - 0.5f) * 20f, (Random.value - 0.5f) * 20f);
					_funnyTicks = 0;
				}
			}
			ERROR_TRACK_BLOCK_END
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

	}
}