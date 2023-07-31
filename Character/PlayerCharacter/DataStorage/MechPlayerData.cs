using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities.General;
using XansTools.Utilities.RW;
using Hud = HUD.HUD;

namespace XansCharacter.Character.PlayerCharacter.DataStorage {

	/// <summary>
	/// Extended properties for the player if they are playing as the mech.
	/// </summary>
	public sealed class MechPlayerData {

		public Player Player => _player.Get();
		private readonly WeakReference<Player> _player = new WeakReference<Player>(null);

		/// <summary>
		/// If true, the on-death explosion has already occurred and should not fire again (as it is prone to doing,
		/// which, as funny as it is, is also very loud and laggy).
		/// </summary>
		public bool HasAlreadyExplodedForDeath { get; set; } = false;

		/// <summary>
		/// The HUD for the mech.
		/// </summary>
		public MechPlayerHUD HUD { get; private set; }

		/// <summary>
		/// The latest source of damage. Check <see cref="DamageTracker.HasExpired"/>!
		/// </summary>
		public DamageTracker LastDamage {
			get => _lastDamage;
			set => _lastDamage = value;
		}
		private DamageTracker _lastDamage = default;

		/// <summary>
		/// Whether or not the mech should be in alert mode, which changes its display properties.
		/// </summary>
		public bool ShouldBeInAlertMode => !LastDamage.HasExpired;
		private int _ticksPresent = 0;


		/// <summary>
		/// The charge of the player's battery. 100 is the maximum, 0 is dead. This value <em>can</em> be set to a value out of this range.
		/// Use <see cref="GetClampedBatteryCharge"/> to filter this value out.
		/// </summary>
		public float BatteryCharge { get; set; }

		/// <summary>
		/// The state of the battery, for visual display. A value of 0 indicates passive, -1 indicates that it is being sapped by something in the world,
		/// and +1 indicates that it is being recharged by something in the world
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException">If the value is set to something other than -1, 0, or 1.</exception>
		public int BatteryState {
			get => _batteryState;
			set {
				if (value < -1 || value > 1) throw new ArgumentOutOfRangeException(nameof(value), "Only -1, 0, and 1 are valid arguments!");
				_batteryState = value;
			}
		}
		private int _batteryState = 0;

		/// <summary>
		/// Returns <see cref="BatteryCharge"/>, but clamped within the range of [0, 100].
		/// </summary>
		/// <returns></returns>
		public float GetClampedBatteryCharge() {
			return Mathf.Clamp(BatteryCharge, 0, 100);
		}

		/// <summary>
		/// Update the storage to track the amount of ticks it has been alive for.
		/// </summary>
		public void Update() {
			_ticksPresent++;

			Player player = Player;
			if (player != null) {
				RoomCamera myCamera = player.GetCamera();
				if (myCamera != null) {
					Hud hud = myCamera.hud;
					hud?.AddPart(new MechPlayerHUD(this, hud));
				}
			}
		}

		public MechPlayerData(Player player) {
			_player.SetTarget(player);
		}

		/// <summary>
		/// This type tracks damage done by a creature.
		/// </summary>
		public readonly struct DamageTracker {

			private readonly WeakReference<MechPlayerData> _parent;
			private readonly WeakReference<BodyChunk> _lastSourceObject;
			private readonly int _startTick;
			private readonly int _ticksToLive;
			private readonly Creature.DamageType _damageType;

			public int TicksRemaining => _ticksToLive - ((_parent.Get()?._ticksPresent ?? int.MaxValue) - _startTick);

			/// <summary>
			/// Whether or not this tracker has expired.
			/// </summary>
			public bool HasExpired => TicksRemaining <= 0;

			/// <summary>
			/// The <see cref="Creature"/> that used the weapon that hurt me. 
			/// This may be null, as this is stored in a weak reference (and not all sources are creatures).
			/// <para/>
			/// This is fundamentally different from <see cref="SourceObject"/>, which contains the direct damage source.
			/// For the case of things like spears or other weapons, this is the creature that threw it, not the spear in and of itself.
			/// </summary>
			public Creature AttackingCreature => (SourceObject as Weapon)?.thrownBy;

			/// <summary>
			/// The <see cref="BodyChunk"/> of the object that dealt damage. Reminder that for weapons, this is the weapon (i.e. this will be the spear that hit the player)
			/// </summary>
			public BodyChunk SourceBodyChunk => _lastSourceObject.Get();

			/// <summary>
			/// The <see cref="PhysicalObject"/> of the object that dealt damage.
			/// </summary>
			public PhysicalObject SourceObject => _lastSourceObject.Get()?.owner;

			/// <summary>
			/// The <see cref="MechPlayerData"/> that this exists for. May be null if it was disposed of.
			/// </summary>
			public MechPlayerData Parent => _parent.Get();

			/// <summary>
			/// The type of damage that the attacker did, or null if there was no damage type.
			/// </summary>
			public Creature.DamageType DamageType => _damageType;

			public DamageTracker(MechPlayerData parent, BodyChunk source, Creature.DamageType damageType, int ttlTicks = 400) {
				_parent = new WeakReference<MechPlayerData>(parent);
				_lastSourceObject = new WeakReference<BodyChunk>(source);
				_startTick = parent._ticksPresent;
				_ticksToLive = ttlTicks;
				_damageType = damageType;
			}

		}
	}
}
