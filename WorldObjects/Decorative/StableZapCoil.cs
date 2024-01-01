#nullable enable
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.RW;
using XansTools.Utilities.RW.SoundObjects;
using static DreamsOfInfiniteGlass.WorldObjects.CustomObjectData;
using Random = UnityEngine.Random;
using System.Diagnostics.CodeAnalysis;

namespace DreamsOfInfiniteGlass.WorldObjects.Decorative {

	/// <summary>
	/// A variation of a superstructure zap coil that does not flicker nor sputter.
	/// It should be used 
	/// </summary>
	public class StableZapCoil : ZapCoil {

		public static Color DEFAULT_COLOR { get; } = new Color(162f/255f, 1.0f, 62f/255f, 0.500f);

		public static Color ALARM_LIGHT_COLOR { get; } = new Color(1.000f, 0.100f, 0.100f, 1.000f);

		/// <summary>
		/// Whether or not this device is effectively powered, which is determined by a number of factors such as <see cref="ZapCoil.powered"/> and the power animation value.
		/// </summary>
		public bool IsEffectivelyPowered => powered && (_powerState > 0) && (_ticksRemainingKnockout == 0);

		/// <summary>
		/// The color of this coil's light. Note that alpha is used as intensity (functions as a brightness multiplier, a bit like the V in HSV).
		/// To directly modify or access the brightness value, use <see cref="Intensity"/>.
		/// </summary>
		public Color ElectricityColor {
			get => _unmodulatedColor;
			set {
				_unmodulatedColor = value;
				_intensity = value.a; 
				_data.color = value;
			}
		}

		/// <summary>
		/// The center position of this effect.
		/// </summary>
		public Vector2 CenterPosition => new Vector2(rect.left + (rect.Width * 0.5f), rect.bottom + (rect.Height * 0.5f));
		// TODO: Shouldn't this be wrong? This is in tile coordinates...

		/// <summary>
		/// A 2D normal describing the direction this is rotated. It is (1, 0) for horizontal, and (0, 1) for vertical.
		/// </summary>
		public Vector2 Normal => horizontalAlignment ? new Vector2(1, 0) : new Vector2(0, 1);

		/// <summary>
		/// The position of the center of the first connector, which is on the negative axis (left or bottom).
		/// </summary>
		public Vector2 ConnectorPositionA {
			get {
				Vector2 add = horizontalAlignment ? new Vector2((rect.Width >> 1) + 2.5f, 0) : new Vector2(0, (rect.Height >> 1) + 2.5f);
				return CenterPosition - add;
			}
		}

		/// <summary>
		/// The position of the center of the second connector, which is on the positive axis (right or top).
		/// </summary>
		public Vector2 ConnectorPositionB {
			get {
				Vector2 add = horizontalAlignment ? new Vector2((rect.Width >> 1) + 2.5f, 0) : new Vector2(0, (rect.Height >> 1) + 2.5f);
				return CenterPosition + add;
			}
		}



		/// <summary>
		/// The base, unmodulated and raw color set by <see cref="ElectricityColor"/>. Alpha may not equal 1.0f here.
		/// </summary>
		private Color _unmodulatedColor = DEFAULT_COLOR;

		/// <summary>
		/// The base intensity of the light. This directly affects <see cref="ElectricityColor"/>'s <see cref="Color.a"/>.
		/// </summary>
		/// <value>(Default) 0.5f</value>
		public float Intensity {
			get => _intensity;
			set {
				_intensity = value;
				_unmodulatedColor.a = value;
			}
		}
		private float _intensity;

		/// <summary>
		/// The intensity of the light, modified by its power state.
		/// </summary>
		public float EffectiveIntensity => Intensity * Mathf.Round(Mathf.Clamp01(_powerState - 0.499f)) * GetFlickerBrightness();

		/// <summary>
		/// Intensity variation determines how much the light pulsates.
		/// </summary>
		/// <value>(Default) 0.025f</value>
		public float IntensityVariation { get; set; } = 0.025f;

		/// <summary>
		/// The rate at which the intensity wobbles by its <see cref="IntensityVariation"/>.
		/// </summary>
		/// <value>(Default) 0.25f</value>
		public float IntensityVariationRate { get; set; } = 0.25f;

		private LightSource _centerLight;
		private LightSource _alarmLight1;
		private LightSource _alarmLight2;

		/// <summary>
		/// An arbitrary measure of time, for some visual effects.
		/// </summary>
		private float _arbTime = 0;

		/// <summary>
		/// The amount of time the alarm sound has been playing for.
		/// </summary>
		private float _alarmPlayTime = 0f;

		/// <summary>
		/// The data associated with the placeable object representing this object.
		/// </summary>
		private ColoredGridRectObjectData _data;

		/// <summary>
		/// Upon being knocked out, this many frames must pass before it can begin its powerup sequence again.
		/// </summary>
		private int _ticksRemainingKnockout = 0;

		/// <summary>
		/// The power animation value determines an interpolated power % for this object, so that it doesn't snap back on.
		/// </summary>
		private float _powerState = 1f;

		/// <summary>
		/// This is used to make the power flicker immediately after turning on.
		/// </summary>
		private int _flicker = 0;

		/// <summary>
		/// Utility method for <see cref="EffectiveIntensity"/>
		/// </summary>
		/// <returns></returns>
		private float GetFlickerBrightness() {
			if (_flicker > 0) {
				if (_flicker.IsOdd()) {
					return 0.75f;
				}
				return 0.50f;
			}
			return 1.00f;
		}

		[Obsolete("The disrupted loop is not available for this class.", true)]

		public new DynamicSoundLoop disruptedLoop {
			[DoesNotReturn]
			get {
				throw new NotSupportedException();
			}
		}

		public StableZapCoil(IntRect rect, ColoredGridRectObjectData data, Room room) : base(rect, room) {
			_data = data; 
			_unmodulatedColor = data.color;
			_intensity = data.color.a;
			_powerState = 1f;

			powered = true;
			soundLoop = new NoDopplerRectangularDynamicSoundLoop(this, GetFloatRect, room);
			soundLoop.sound = Sounds.CONDUIT_BUZZ_LOOP;
			((ZapCoil)this).disruptedLoop = null; // Cast to ZapCoil to avoid the Obsolete error.
			_centerLight = new LightSource(CenterPosition.TileToWorldCoord(), false, data.color, this);
			_centerLight.HardSetRad(rect.Area);

			// TODO: Why do these not work?
			_alarmLight1 = new LightSource(ConnectorPositionA.TileToWorldCoord(), false, ALARM_LIGHT_COLOR, this, true);
			_alarmLight2 = new LightSource(ConnectorPositionB.TileToWorldCoord(), false, ALARM_LIGHT_COLOR, this, true);
			_alarmLight1.HardSetRad(5f);
			_alarmLight2.HardSetRad(5f);

			room.AddObject(_centerLight);
			room.AddObject(_alarmLight1);
			room.AddObject(_alarmLight2);
		}

		private float GetIntensityNow() {
			float cosCentered = Mathf.Cos(_arbTime * IntensityVariationRate) * 0.5f; // magnitude=1, centered around 0.
			cosCentered *= IntensityVariation;
			return Mathf.Clamp01(EffectiveIntensity + cosCentered);
		}

		public override void Update(bool eu) {
			evenUpdate = eu; // This is what calling base does (that matters, at least)

			// Maybe this will help
			soundLoop.Update();
			disruption = 0;
			smoothDisruption = 0;
			soundLoop.Volume = turnedOn * 0.2f;
			zapLit = 1;

			_unmodulatedColor = _data.color;
			_intensity = _data.color.a;
			Color copy = ElectricityColor;
			copy.a = GetIntensityNow();
			_centerLight.color = copy.AlphaAsIntensity();

			if (_powerState < 0) {
				// The interval is 0.65 seconds.
				if (_alarmPlayTime <= 0) {
					Color alarmColor = ALARM_LIGHT_COLOR.AlphaAsIntensity();
					_alarmLight1.color = alarmColor;
					_alarmLight2.color = alarmColor;
					_alarmPlayTime = 0.65f;
				} else {
					Color alarmColor = _alarmLight1.color;
					alarmColor.a = 0.9f;
					alarmColor = alarmColor.AlphaAsIntensity();
					_alarmLight1.color = alarmColor;
					_alarmLight2.color = alarmColor;
				}
				_alarmPlayTime -= Mathematical.RW_DELTA_TIME;
			} else {
				_alarmPlayTime = 0f;
				Color alarmColor = _alarmLight1.color;
				alarmColor.a = 0.5f;
				alarmColor = alarmColor.AlphaAsIntensity();
				_alarmLight1.color = alarmColor;
				_alarmLight2.color = alarmColor;
			}

			if (IsEffectivelyPowered && _powerState > 0.5f) {
				for (int physObjCtrIdx = 0; physObjCtrIdx < room.physicalObjects.Length; physObjCtrIdx++) {
					List<PhysicalObject> physObjects = room.physicalObjects[physObjCtrIdx];
					for (int objIdx = 0; objIdx < physObjects.Count; objIdx++) {
						PhysicalObject physicsObject = physObjects[objIdx];
						for (int partIdx = 0; partIdx < physicsObject.bodyChunks.Length; partIdx++) {
							BodyChunk chunk = physicsObject.bodyChunks[partIdx];
							if ((horizontalAlignment && chunk.ContactPoint.y != 0) || (!horizontalAlignment && chunk.ContactPoint.x != 0)) {
								Vector2 contact = chunk.ContactPoint.ToVector2();
								Vector2 contactArea = chunk.pos + contact * (chunk.rad + 30f);
								if (GetFloatRect.Vector2Inside(contactArea)) {
									TriggerSpecialZap(chunk.pos + contact * chunk.rad, chunk.rad);
									chunk.vel -= (contact * 6f + Custom.RNV() * Random.value) / chunk.mass;
									if (physicsObject is Creature creature) {
										if (creature is Player player && Extensible.Player.Binder<MechPlayer>.TryGetBinding(player, out MechPlayer mech)) {
											mech.Die(true);
										} else {
											creature.Die();
										}
									}
									if (ModManager.MSC && physicsObject is ElectricSpear spear) {
										spear.Recharge();
									}
								}
							}
						}
					}
				}
			}

			// TO FUTURE XAN BEFORE YOU THROW A FIT ABOUT NO DELTA TIME
			// Rain World always updates at 1/40th of a second. Too much lag? Game slows down. No accounting for time.
			// A tick is ALWAYS 1/40th of a second. No exceptions.

			const float baseWidth = 0.5f;
			float mod1 = Mathf.Sin(_arbTime * IntensityVariationRate) * IntensityVariation;
			float mod2 = Mathf.Cos(_arbTime * IntensityVariationRate) * IntensityVariation;
			for (int i = 0; i < flicker.GetLength(0); i++) {
				flicker[i, 0] = baseWidth + mod1;
				flicker[i, 1] = baseWidth + mod2;
				flicker[i, 2] = baseWidth + mod1;
				flicker[i, 3] = baseWidth + mod2;
			}
			_arbTime += Mathematical.RW_DELTA_TIME;

			if (_flicker > 0) {
				_flicker--;
			}
			if (_ticksRemainingKnockout > 0) {
				_ticksRemainingKnockout--;
				if (_ticksRemainingKnockout == 60) {
					// The value of 60 is used due to timing. A value of 0 causes the charge to play for ~1.5s after the flicker (see below) plays.
					// 60 ticks is about 1.5 seconds
					// Play a sound to indicate recharging.
					room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, CenterPosition, 0.5f, 1.5f);
				}
			} else if (_powerState < 1) {
				// Can add power here.
				_powerState += 1f / 80f;
				if (_powerState >= 1) {
					_powerState = 1;
					room.PlaySound(SoundID.Mouse_Light_Flicker, CenterPosition, 2f, 1f);
					_flicker = 4;
				}
			}
		}

		public void TriggerSpecialZap(Vector2 zapContact, float massRad) {
			room.AddObject(new ColoredZapFlash(zapContact, Mathf.InverseLerp(-0.05f, 15f, massRad), ElectricityColor));
			room.PlaySoundNoDoppler(SoundID.Zapper_Zap, zapContact, 1f, 1f);
			room.PlaySoundNoDoppler(Sounds.STOCK_ZAP_SOUND, zapContact, 1f, 1.5f);
			room.PlaySoundNoDoppler(Sounds.INDUSTRIAL_ALARM, zapContact, 0.5f, 1.0f);
			_powerState = 0;
			_ticksRemainingKnockout = Mathematical.SecondsToTicks(5.0f);

			Color alarmColor = ALARM_LIGHT_COLOR.AlphaAsIntensity();
			_alarmLight1.color = alarmColor;
			_alarmLight2.color = alarmColor;
		}

		public void TriggerSpecialZap(Vector2 zapContact, float massRad, bool noAlarm) {
			room.AddObject(new ColoredZapFlash(zapContact, Mathf.InverseLerp(-0.05f, 15f, massRad), ElectricityColor));
			room.PlaySoundNoDoppler(SoundID.Zapper_Zap, zapContact, 1f, 1f);
			room.PlaySoundNoDoppler(Sounds.STOCK_ZAP_SOUND, zapContact, 1f, 1.5f);
			if (!noAlarm) room.PlaySoundNoDoppler(Sounds.INDUSTRIAL_ALARM, zapContact, 0.5f, 1.0f);
			_powerState = 0;
			_ticksRemainingKnockout = Mathematical.SecondsToTicks(5.0f);

			Color alarmColor = ALARM_LIGHT_COLOR.AlphaAsIntensity();
			_alarmLight1.color = alarmColor;
			_alarmLight2.color = alarmColor;
		}


		public class ColoredZapFlash : ZapFlash {

			public Color color;

			public ColoredZapFlash(Vector2 initPos, float size, Color color) : base(initPos, size) {
				this.color = color;
			}

			public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
				base.InitiateSprites(sLeaser, rCam);
				sLeaser.sprites[0].color = color;
			}

			public override void Update(bool eu) {
				// No base.update call --
				// base CosmeticSprite:
				lastPos = pos;
				pos += vel;
				// base UpdatableAndDeletable:
				evenUpdate = eu;

				if (lightsource == null) {
					lightsource = new LightSource(pos, false, color, this);
					room.AddObject(lightsource);
				} else {
					if (color != lightsource.color) {
						lightsource.color = color;
					}
				}
				lastLife = life;
				life -= 1f / lifeTime;
				if (lastLife < 0f) {
					lightsource?.Destroy();
					Destroy();
				}
			}
		}
	}
}
