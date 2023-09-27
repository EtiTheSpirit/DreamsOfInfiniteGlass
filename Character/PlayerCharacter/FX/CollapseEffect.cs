#nullable enable
using MoreSlugcats;
using Noise;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.RW;
using Random = UnityEngine.Random;
using XansTools.Utilities.General;
using DreamsOfInfiniteGlass.WorldObjects.Physics;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter.FX {
	public class CollapseEffect : UpdatableAndDeletable {
		
		public Color CollapseColor { get; set; } = new Color(0.2f, 1.0f, 0.2f);

		private bool _stop = false;
		private int _ticksLive = 0;
		private int _specialChance = TICKS_UNTIL_DESTROY;
		private Vector2 _at;
		private Creature? _src;

		private const int PRE_EXPLODE_AT = 110;
		private const int TICKS_UNTIL_MAIN_EXPLODE = 100;
		private const int TICKS_UNTIL_DESTROY = 200;

		private LightningBolt[] _bolts = new LightningBolt[10];
		private int _currentBolts = 0;

		/// <summary>
		/// This is set to true upon the detonation going off. Delete the player on the Update() where this is true.
		/// </summary>
		public bool DetonationCompleted { get; private set; }

		public CollapseEffect(Room room, Vector2 at, Creature? causedBy = null) {
			this.room = room;
			_src = causedBy;
			_at = at;
		}

		public CollapseEffect(MechPlayer onPlayer) : this(onPlayer.room, onPlayer.firstChunk.pos, onPlayer) { }

		public void UpdatePosition(ref CollapseEffect? fieldStoringThis, Vector2 position) {
			_at = position;
			if (_stop) fieldStoringThis = null;
		}

		public override void Update(bool eu) {
			if (_stop) return;
			if (slatedForDeletetion) return;

			if (_ticksLive == 0 || _ticksLive == 40 || _ticksLive == 80 || _ticksLive == 120) {
				room.PlaySound(Sounds.DEVICE_WARNING_LOOP, _at, 0.35f, 1.0f);
			} else if (_ticksLive == PRE_EXPLODE_AT) {
				PreExplode();
			} else if (_ticksLive == PRE_EXPLODE_AT + TICKS_UNTIL_MAIN_EXPLODE) {
				Explode();
				DetonationCompleted = true;
			} else if (_ticksLive == PRE_EXPLODE_AT + TICKS_UNTIL_MAIN_EXPLODE + TICKS_UNTIL_DESTROY) {
				_stop = true;
				Destroy();
			}

			if (_ticksLive >= 40 && _ticksLive < 120) {
				float chance = ((_ticksLive - 40f) / 80f) * 0.1f;
				if (Random.value <= 0.1f + chance) {
					Color brightnessModifiedSparkClr = Color.Lerp(CollapseColor, new Color(1f, 1f, 1f), Random.value);
					room.AddObject(new Spark(_at, Custom.RNV() * 160f, brightnessModifiedSparkClr, null, 11, 28));
					room.PlaySound(SoundID.SS_Mycelia_Spark, _at, 1.8f, (Random.value * 0.1f) + 0.9f);
				}
			}
			if (_ticksLive >= PRE_EXPLODE_AT && _ticksLive < PRE_EXPLODE_AT + TICKS_UNTIL_MAIN_EXPLODE) {
				if (Random.value <= 0.09f) {
					room.AddObject(new SingularityBomb.SparkFlash(_at, 250f + Random.value * 20f, new Color(0f, 1f, 0f)));
				}
				if (eu) {
					if (_currentBolts < _bolts.Length && Random.value <= 0.125f) {
						LightningBolt lb = new LightningBolt(_at, _at + Custom.RNV() * ((Random.value * 30f) + 40f), 1, Random.value + 20f) {
							color = CollapseColor
						};
						_bolts[_currentBolts++] = lb;
						room.AddObject(lb);
					}
					for (int i = 0; i < _currentBolts; i++) {
						_bolts[i].intensity -= Random.value * 0.05f;
					}
					
					room.PlaySound(SoundID.SS_Mycelia_Spark, _at, 1.8f, (Random.value * 0.1f) + 0.9f);
				}
			}
			if (_ticksLive >= PRE_EXPLODE_AT + TICKS_UNTIL_MAIN_EXPLODE && _ticksLive < PRE_EXPLODE_AT + TICKS_UNTIL_MAIN_EXPLODE + TICKS_UNTIL_DESTROY) {
				for (int i = 0; i < _currentBolts; i++) {
					_bolts[i].Destroy();
				}
				_currentBolts = 0;
				float chance = (_specialChance / 200f) + 0.2f;
				if (Random.value <= chance) {
					room.AddObject(new LightningBolt(_at, _at + Custom.RNV() * ((Random.value * 90f) + 25f), 0, Random.value + 60f) {
						color = CollapseColor
					});
				}
			}

			_ticksLive++;
		}

		private void PreExplode() {
			room.PlaySound(Sounds.SINGULARITY_MERGED_CHARGE, _at, 0.7f, 1f + ((Random.value - 0.5f) * 0.1f));
			room.AddObject(new SingularityBomb.SparkFlash(_at, 100f, new Color(0f, 1f, 0f)));

			for (int i = 0; i < 25; i++) {
				Vector2 randomNrm = Custom.RNV();
				if (room.GetTile(_at + randomNrm * 20f).Solid) {
					if (!room.GetTile(_at - randomNrm * 20f).Solid) {
						randomNrm *= -1f;
					} else {
						randomNrm = Custom.RNV();
					}
				}
				for (int j = 0; j < 3; j++) {
					Color brightnessModifiedSparkClr = Color.Lerp(CollapseColor, new Color(1f, 1f, 1f), Random.value);
					float rng1 = (Random.value * 30f) + 30f;
					float rng2 = (Random.value * 31f) + 7f;
					float rng3 = Random.value * 20f;

					Vector2 position = _at + (randomNrm * rng1);
					Vector2 velocity = (randomNrm * rng2) + (Custom.RNV() * rng3);
					room.AddObject(new Spark(position, velocity, brightnessModifiedSparkClr, null, 11, 28));
				}

				float rng = Random.value;
				Vector2 deviatedPosition = randomNrm * (40f * Random.value);
				room.AddObject(new Explosion.FlashingSmoke(
					_at + deviatedPosition, 
					randomNrm * Mathf.Lerp(4f, 20f, rng * rng), 
					2f + (0.05f * Random.value), 
					new Color(1f, 1f, 1f), 
					CollapseColor, 
					Random.Range(3, 11)
				));
			}
			room.AddObject(new ShockWave(_at, 95f, 0.010f, 10, false));
			room.AddObject(new Explosion.ExplosionLight(_at, 280f, 1f, 7, CollapseColor));
			room.AddObject(new Explosion.ExplosionLight(_at, 230f, 1f, 3, new Color(1f, 1f, 1f)));
			room.AddObject(new Explosion.ExplosionLight(_at, 2000f, 2f, 10, CollapseColor));

			FirecrackerPlant.ScareObject scareObj = new FirecrackerPlant.ScareObject(_at);
			scareObj.fearRange = 12000f;
			scareObj.lifeTime = -700;
			scareObj.fearScavs = true;
			room.AddObject(scareObj);
		}

		/// <summary>
		/// Causes a singularity bomb effect.
		/// </summary>
		private void Explode() {
			room.AddObject(new Explosion(room, null, _at, 7, 450f, 6.2f, 10f, 280f, 0.25f, _src, 1f, 160f, 1f));
			room.AddObject(new Explosion(room, null, _at, 7, 2000f, 4f, 0f, 400f, 0.25f, _src, 1f, 200f, 1f));
			room.AddObject(new Explosion(room, null, _at, 7, 4000f, 4f, 0f, 400f, 0.25f, _src, 1f, 200f, 1f));

			room.AddObject(new ShockWave(_at, 350f, 0.185f, 100, true));
			room.AddObject(new ShockWave(_at, 2000f, 0.050f, 70, false));
			room.AddObject(new ShockWave(_at, 5000f, 0.050f, 30, false));

			room.ScreenMovement(_at, default, 0.9f);
			room.PlaySoundNoDoppler(SoundID.Bomb_Explode, _at, 1.1f, 1.0f);
			room.PlaySound(Sounds.GIGA_BOOM, _at, 0.75f, 1.0f);
			room.AddObject(new SingularityBomb.SparkFlash(_at, 1000f, new Color(0.1f, 1f, 0f, 0.75f)));

			for (int i = 0; i < 40; i++) {
				Vector2 randomNrm = Custom.RNV();
				if (room.GetTile(_at + randomNrm * 20f).Solid) {
					if (!room.GetTile(_at - randomNrm * 20f).Solid) {
						randomNrm *= -1f;
					} else {
						randomNrm = Custom.RNV();
					}
				}
				for (int j = 0; j < 3; j++) {
					Color brightnessModifiedSparkClr = Color.Lerp(CollapseColor, new Color(1f, 1f, 1f), Random.value);
					float rng1 = (Random.value * 30f) + 30f;
					float rng2 = (Random.value * 31f) + 7f;
					float rng3 = Random.value * 20f;

					Vector2 position = _at + (randomNrm * rng1);
					Vector2 velocity = (randomNrm * rng2) + (Custom.RNV() * rng3);
					room.AddObject(new Spark(position, velocity, brightnessModifiedSparkClr, null, 50, 100));
					room.AddObject(new LightningBolt(_at, _at + Custom.RNV() * ((Random.value * 90f) + 25f), 0, Random.value + 60f) {
						color = CollapseColor
					});
				}
				if (i % 10 == 0) {
					room.AddObject(new SingularityBomb.SparkFlash(_at, 250f + Random.value * 50f, new Color(0f, 1f, 0f)));
				}
			}

			for (int physObjIdx = 0; physObjIdx < room.physicalObjects.Length; physObjIdx++) {
				List<PhysicalObject> pool = room.physicalObjects[physObjIdx];
				for (int physObjSubIdx = 0; physObjSubIdx < pool.Count; physObjSubIdx++) {
					PhysicalObject obj = pool[physObjSubIdx];
					if (obj is Creature creature && Custom.Dist(obj.firstChunk.pos, _at) < 175f) {
						creature.Die();
					}
					if (obj is ElectricSpear spear) {
						if (spear.abstractSpear.electricCharge == 0) {
							spear.Recharge();
						} else {
							spear.ExplosiveShortCircuit();
						}
					}
				}
			}
			FirecrackerPlant.ScareObject scareObj = new FirecrackerPlant.ScareObject(_at);
			scareObj.fearRange = 12000f;
			scareObj.lifeTime = -600;
			scareObj.fearScavs = true;
			room.AddObject(scareObj);
			room.InGameNoise(new InGameNoise(_at, 12000f, _src, 1f));
		}
	}
}
