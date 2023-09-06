using System;
using HUD;
using UnityEngine;
using RWCustom;
using DreamsOfInfiniteGlass.Character.NPC.Iterator.Interaction;
using GameHUD = HUD.HUD;
using Random = UnityEngine.Random;
using Music;
using XansTools.Utilities.General;
// using static XansCharacter.Character.NPC.Iterator.Interaction.GlassConversations;

namespace DreamsOfInfiniteGlass.Character.NPC.Iterator {
	public class GlassOracleBehavior : OracleBehavior, Conversation.IOwnAConversation, GlassConversation.IParameterizedEventReceiver {

		public new GlassOracle oracle => _glass.Get();
		private WeakReference<GlassOracle> _glass;

		public bool IsBusyProcessing { get; set; }

		public override DialogBox dialogBox {
			get {
				GameHUD hud = oracle.room.game.cameras[0].hud;
				if (hud.dialogBox == null) {
					hud.InitDialogBox();
					hud.dialogBox.defaultYPos = -10f;
				}
				return hud.dialogBox;
			}
		}

		/// <summary>
		/// The exact center of the movable area within the room, in pixel coordinates.
		/// </summary>
		public Vector2 RoomCenter {
			get {
				if (_roomCenterCache == null) {
					_roomCenterCache = Vector2.Lerp(oracle.arm.cornerPositions[3], oracle.arm.cornerPositions[1], 0.5f);
					// 0 is a corner, 2 is a corner on the opposite side (the pattern follows a ] shape)
					// [0] [1]
					// [3] [2]
				}
				return _roomCenterCache.Value;
			}
		}
		private Vector2? _roomCenterCache;

		/// <summary>
		/// The exact bounds of this room in pixels. This is used to move the umbilical carriage and also to
		/// limit the sparks from exiting the room (which looks weird).
		/// </summary>
		public Rect RoomBounds {
			get {
				if (_roomBoundsCache == null) {
					Vector2 min = oracle.arm.cornerPositions[3];
					Vector2 max = oracle.arm.cornerPositions[1];
					Vector2 center = RoomCenter;
					float w = max.x - min.x;
					float h = max.y - min.y;
					_roomBoundsCache = new Rect(center.x, center.y, w, h);
				}
				return _roomBoundsCache.Value;
			}
		}
		private Rect? _roomBoundsCache;

		public override Vector2 OracleGetToPos => ClampVectorInRoom(_currentGetTo);
		public override Vector2 BaseGetToPos => _baseIdeal;
		public override Vector2 GetToDir => new Vector2(0f, 1f);
		public override bool EyesClosed => _eyesClosed;
		private bool _eyesClosed = false;

		private Vector2 _origin;
		private Vector2 _baseIdeal;
		private Vector2 _lastPos;
		private Vector2 _nextPos;
		private Vector2 _lastPosHandle;
		private Vector2 _nextPosHandle;
		private Vector2 _currentGetTo;
		private float _pathProgression;
		private Conversation _currentConversation;
		private bool _tempRuntimePreventMoreConversation = false;
		//#warning Conversation will never occur.
		private int _moveTimer = 0;

		/// <summary>
		/// The activity of the mycelia connections (the sparky bits).
		/// Negative values return it to automatic behavior. A value of 0 prevents them from firing entirely, and a value of 1 guarantees they fire every frame.
		/// </summary>
		public float CurrentConnectionActivity { get; set; } = -1;

		public GlassOracleBehavior(GlassOracle glass) : base(glass) {
			_glass = new WeakReference<GlassOracle>(glass);
			_origin = oracle.firstChunk.pos;
			SetNewDestination(_origin);
		}

		private void SetNewDestination(Vector2 dst) {
			_lastPos = _currentGetTo;
			_nextPos = dst;
			_lastPosHandle = Custom.RNV() * Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(_lastPos, _nextPos);
			_nextPosHandle = -GetToDir * Mathf.Lerp(0.3f, 0.65f, Random.value) * Vector2.Distance(_lastPos, _nextPos);
			_pathProgression = 0f;
		}

		private Vector2 ClampVectorInRoom(Vector2 v) {
			Vector2 vector = v;
			vector.x = Mathf.Clamp(vector.x, oracle.arm.cornerPositions[0].x + 10f, oracle.arm.cornerPositions[1].x - 10f);
			vector.y = Mathf.Clamp(vector.y, oracle.arm.cornerPositions[2].y + 10f, oracle.arm.cornerPositions[1].y - 10f);
			return vector;
		}

		private void PrepareRoomForConversation() {
			if (oracle.room.lockedShortcuts.Count == 0) {
				for (int i = 0; i < oracle.room.shortcutsIndex.Length; i++) {
					oracle.room.lockedShortcuts.Add(oracle.room.shortcutsIndex[i]);
				}
			}

			for (int index = 0; index < oracle.room.updateList.Count; index++) {
				if (oracle.room.updateList[index] is AntiGravity antiGrav) {
					antiGrav.active = false; // Disable the anti-gravity object so I can manually control it.
				}
			}
			oracle.room.gravity = 0.5f;
		}

		private void UnlockRoomAfterConversation() {
			oracle.room.lockedShortcuts.Clear();

			for (int index = 0; index < oracle.room.updateList.Count; index++) {
				if (oracle.room.updateList[index] is AntiGravity antiGrav) {
					antiGrav.active = false; // This would normally be true.
				}
			}
			oracle.room.gravity = 0.1f; // This would normally be 0f.
		}

		private float BasePosScore(Vector2 tryPos) {
			return Mathf.Abs(Vector2.Distance(_nextPos, tryPos) - 200f) + Custom.LerpMap(Vector2.Distance(player.DangerPos, tryPos), 40f, 300f, 800f, 0f);
		}

		public override void Update(bool eu) {
			base.Update(eu);
			if (player == null) return;
			if (player.room != oracle.room) return;

			oracle.graphicsModule?.Update();
			_pathProgression += 0.01f;
			_currentGetTo = Custom.Bezier(_lastPos, ClampVectorInRoom(_lastPos + _lastPosHandle), _nextPos, ClampVectorInRoom(_nextPos + _nextPosHandle), _pathProgression);

			consistentBasePosCounter++;
			if (oracle.room.readyForAI) {
				Vector2 randomPosition = new Vector2(Random.value * oracle.room.PixelWidth, Random.value * oracle.room.PixelHeight);
				if (!oracle.room.GetTile(randomPosition).Solid && BasePosScore(randomPosition) + 40f < BasePosScore(_baseIdeal)) {
					_baseIdeal = randomPosition;
					consistentBasePosCounter = 0;
				}
			} else {
				_baseIdeal = _nextPos;
			}

			if (player != null) {
				Vector2 playerPosition = player.DangerPos;
				if (_moveTimer <= 0) {
					_moveTimer = 240;
					Vector2 newDesiredPosition = RoomCenter;
					newDesiredPosition.x += (Random.value - 0.5f) * 520f; // 560 is the width of the room. Add some margin.
					newDesiredPosition.y += Random.value * 200f; // A random height in the upper half of the room.
					SetNewDestination(newDesiredPosition + (Custom.RNV() * 40));
				}
				_moveTimer--;

				lookPoint = playerPosition;
			}

			if (_currentConversation != null) {
				if (player == null || player.room != oracle.room) {
					_currentConversation.Interrupt(". . . ", 0);
					_currentConversation.Destroy();
				}
				_currentConversation.Update();
				if (_currentConversation.slatedForDeletion) {
					_tempRuntimePreventMoreConversation = true;
					_currentConversation = null;
					UnlockRoomAfterConversation();
				}
			} else {
				if (player != null) {
					//if (!Glass.HasTalkedBefore && !_tempRuntimePreventMoreConversation) {
					if (!_tempRuntimePreventMoreConversation) {
						_tempRuntimePreventMoreConversation = true;
						Log.LogDebug("Started conversation.");
						if (_currentConversation != null) {
							_currentConversation.Interrupt(". . . ", 0);
							_currentConversation.Destroy();
							ProcessManager mgr = oracle.room.game.manager;
							MusicPlayer plr = mgr.musicPlayer;
							if (plr.song != null) {
								plr.song.FadeOut(100f);
							}
						}
						_currentConversation = GlassConversations.ImBack(this);
						_currentConversation.AddEvents();
						PrepareRoomForConversation();
					}
				}
			}
		}

		public void EventFired(GlassConversation.ParameterizedEvent evt) {
			if (evt.EventName == "FuckingDie") {
				evt.TryGetParameterAs("funnyRagdoll", out bool funny);
				oracle.FuckingDie(funny);
			} else if (evt.EventName == "SetBrainActivity") {
				if (evt.TryGetParameterAs("level", out float level)) {
					CurrentConnectionActivity = level;
				}
			} else if (evt.EventName == "SetProcessing") {
				if (evt.TryGetParameterAs("processing", out bool processing)) {
					IsBusyProcessing = processing;
				}
			}
		}
	}
}
