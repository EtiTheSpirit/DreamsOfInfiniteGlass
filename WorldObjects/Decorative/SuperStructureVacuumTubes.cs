using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static DreamsOfInfiniteGlass.WorldObjects.CustomObjectData;
using Random = UnityEngine.Random;

namespace DreamsOfInfiniteGlass.WorldObjects.Decorative {

	/// <summary>
	/// An alternate SuperStructureFuses that appears more like electronic vacuum tubes
	/// </summary>
	public class SuperStructureVacuumTubes : UpdatableAndDeletable, IDrawable {

		/// <summary>
		/// Using depth as an index here will provide the proper sprite alpha value.
		/// </summary>
		private static readonly float[] ALPHAS_AT_DEPTH = new float[] {
			1f - (0.5f / 30f),
			1f - (11f / 30f),
			1f - (21f / 30f)
		};

		/// <summary>
		/// Returns the bounding box of this object in pixel space, acquired by its tile space bounds.
		/// </summary>
		public FloatRect PixelSpaceRect => new FloatRect(_tileSpaceRect.left * 20f, _tileSpaceRect.bottom * 20f, _tileSpaceRect.right * 20f + 20f, _tileSpaceRect.top * 20f + 20f);

		/// <summary>
		/// Whether or not this object is powered.
		/// </summary>
		public bool Powered { get; set; }

		/// <summary>
		/// The rectangle that this object occupies, in tile space.
		/// </summary>
		private IntRect _tileSpaceRect;

		/// <summary>
		/// The placed object that this operates within.
		/// </summary>
		private PlacedObject _placedObject;

		/// <summary>
		/// Each and every individual tube.
		/// </summary>
		private readonly Activation[,] _activations;

		/// <summary>
		/// The width of the <see cref="_activations"/> array.
		/// </summary>
		private readonly int _width;

		/// <summary>
		/// The height of the <see cref="_activations"/> array.
		/// </summary>
		private readonly int _height;

		/// <summary>
		/// The tile depth corresponds to its layer, save for the fact that Layer 3 and Sky (no tile at all) are treated the same.
		/// 0 is layer 1, 1 is layer 2, 2 is layer 3/sky
		/// </summary>
		private readonly int _tileDepth;

		/// <summary>
		/// The current stored frame index, used by some animations.
		/// </summary>
		private int _frame;

		/// <summary>
		/// Whether or not this effect is visible.
		/// </summary>
		private bool _effectIsVisible;

		private SkinnedGridRectObjectData _data;

		public SkinType Skin {
			get => (SkinType)_data.skin;
			set => _data.skin = (int)value;
		}

		public AnimationType Animation {
			get => (AnimationType)_data.animation;
			set => _data.animation = (int)value;
		}

		public SuperStructureVacuumTubes(PlacedObject placedObject, SkinnedGridRectObjectData data, Room room) {
			_data = data;
			Powered = true;

			IntRect rect = data.Rect;
			_placedObject = placedObject;
			_tileSpaceRect = rect;
			_width = rect.Width << 1;
			_height = rect.Height << 1;
			_activations = new Activation[_width, _height]; // I am not sure why its x2

			// Mimic the code from SSFuse by building a tile array and storing the lowest known depth.
			int depth = 0;
			for (int x = rect.left; x <= rect.right; x++) {
				for (int y = rect.bottom; y <= rect.top; y++) {
					Room.Tile tile = room.GetTile(x, y);
					if (!tile.Solid) {
						// Non-solid indicates layer 2 or 3.
						int layerValue = tile.wallbehind ? 1 : 2;
						if (layerValue > depth) {
							depth = layerValue; // This enforces that it uses the deepest tile that the effect encompasses.
												// This allows the effect to be placed on something like layer 2, and have a cover on top on layer 1 to mask it out.
						}
					}
				}
			}
			_tileDepth = depth;

		}

		/// <summary>
		/// Determines if this effect is visible on-screen by checking each corner. This means that effects larger than the screen will be culled.
		/// </summary>
		private bool IsCulled() {
			for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++) {
				if (room.ViewedByAnyCamera(PixelSpaceRect.GetCorner(cornerIndex), 40f)) {
					return false;
				}
			}
			return true;
		}

		public override void Update(bool eu) {
			base.Update(eu);
			bool isCulled = IsCulled();
			_effectIsVisible = !isCulled;
			if (isCulled) return;
		}


		public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			sLeaser.sprites = new FSprite[_width * _height];
			int index = 0;
			for (int x = 0; x < _width; x++) {
				for (int y = 0; y < _height; y++) {
					FSprite sprite = new FSprite("Futile_White", true);
					sprite.scale = 0.625f;
					sprite.shader = rCam.room.game.rainWorld.Shaders["CustomDepth"];
					sprite.alpha = ALPHAS_AT_DEPTH[_tileDepth];
					sLeaser.sprites[index++] = sprite;
				}
			}
			AddToContainer(sLeaser, rCam, null);
		}

		public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			if (_effectIsVisible != sLeaser.sprites[0].isVisible) {
				for (int i = 0; i < sLeaser.sprites.Length; i++) {
					sLeaser.sprites[i].isVisible = _effectIsVisible;
				}
			}
			if (!_effectIsVisible) return;

			Vector2 thisPos = _placedObject.pos;
			int index = 0;
			for (int x = 0; x < _width; x++) {
				for (int y = 0; y < _height; y++) {
					// When computing the depth, the vanilla code is wrong and uses (10 * depth) - 4
					// The -4 is the problem: While this works for L1/L2, L3 is incorrect (it needs to be 12, but this results in 16).
					int subFactor = 4 << (_tileDepth >> 1);
					// depth shr 1 spits out 1 iff depth is on layer 3/sky, 0 otherwise.
					// 4 shr (above) will result in 4, or 8 on layer 3/sky.
					// By applying this to the camera's depth below, it resolves the bug preventing it from aligning with L3.
					float cameraDepth = _tileDepth * 10 - subFactor;
					Vector2 absPosition = rCam.ApplyDepth(thisPos + new Vector2(x * 10 + 5, y * 10 + 5), cameraDepth);
					absPosition -= camPos;

					FSprite sprite = sLeaser.sprites[index++];
					ref Activation activation = ref _activations[x, y];
					activation.Animate(Animation, Powered, _frame);
					sprite.SetPosition(absPosition);
					sprite.color = activation.GetColor(Skin);
					/*
					ref Activation activation = ref _activations[x, y];
					float flux = (Random.value * 0.0125f) + 0.0025f;
					if (Powered) {
						if (--activation.remainingCurrentStateFrames <= 0) {
							activation.stateIsEnabled = !activation.stateIsEnabled;
							activation.remainingCurrentStateFrames = Mathf.RoundToInt(Random.value * 400) + 400;
						}
						if (!activation.stateIsEnabled) {
							flux *= -2;
						}
						if ((activation.remainingCurrentStateFrames & 0b11111) == 0b10000) {
							// Update this once every 16 frames
							activation.electricalUsage = Random.value * 0.95f;
						}
						activation.electricalUsageFlux = Random.value * 0.05f;
					} else {
						flux *= -2;
					}
					activation.tubeHeat = Mathf.Clamp01(activation.tubeHeat + flux);
					sprite.SetPosition(absPosition);
					sprite.color = activation.AsColor();
					*/
				}
			}
			if (slatedForDeletetion || room != rCam.room) {
				sLeaser.CleanSpritesAndRemove();
			}
			_frame++;
		}

		public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) { }

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
			if (newContatiner == null) {
				newContatiner = rCam.ReturnFContainer("Foreground");
			}
			for (int i = 0; i < sLeaser.sprites.Length; i++) {
				sLeaser.sprites[i].RemoveFromContainer();
				newContatiner.AddChild(sLeaser.sprites[i]);
			}
		}


		/// <summary>
		/// Represents the activation of a vacuum tube.
		/// </summary>
		public struct Activation {
			/// <summary>
			/// The "heat" of the vacuum tube effect, an abstract percentage where 0 means off, and 1 means "as hot as it will get".
			/// </summary>
			public float tubeHeat;

			/// <summary>
			/// A secondary value representing the activation of the tube.
			/// </summary>
			public float electricalUsage;

			/// <summary>
			/// A secondary value representing the activation of the tube.
			/// </summary>
			public float electricalUsageFlux;

			/// <summary>
			/// How many frames this has left before it toggles itself.
			/// </summary>
			public int remainingCurrentStateFrames;

			/// <summary>
			/// If true, the object wants to be powered and glow, if false, it will turn itself off.
			/// </summary>
			public bool stateIsEnabled;

			#region Color Types
			public Color AsBlackbodyColor() {
				// The tube heat should mimic blackbody colors. 
				float r = tubeHeat;
				float g = Mathf.Pow(tubeHeat, 2) * 0.325f;
				float b = 0;
				if (stateIsEnabled) {
					float flux = (electricalUsage + electricalUsageFlux) * 0.2f;
					g += flux * 0.75f;
					b = flux;
				}
				return new Color(r, g, b);
			}

			public Color AsPlasmaColor() {
				float r = tubeHeat * 0.125f;
				float g = tubeHeat;
				float b = tubeHeat * 0.75f;
				if (stateIsEnabled) {
					float flux = (electricalUsage + electricalUsageFlux) * 0.6f;
					r += (Mathf.Round(flux) * 0.5f);
				}
				return new Color(r * 0.75f, g * 0.75f, b * 0.75f);
			}

			public Color AsFuseColor() {
				return new Color(0, 0, tubeHeat);
			}

			public Color AsFuseColor2() {
				return new Color(tubeHeat * electricalUsageFlux * 4f, tubeHeat * 0.25f, tubeHeat * 0.2f);
			}
			#endregion

			#region Animations
			public void AnimateAsEmber(bool powered, int frame) {
				// The tube heat should render a bright greenish-cyan, arbitrary energetic look.
				float flux = (Random.value * 0.0125f) + 0.0025f;
				if (powered) {
					if (--remainingCurrentStateFrames <= 0) {
						stateIsEnabled = !stateIsEnabled;
						remainingCurrentStateFrames = Mathf.RoundToInt(Random.value * 400) + 400;
					}
					if (!stateIsEnabled) {
						flux *= -2;
					}
					if ((remainingCurrentStateFrames & 0b11111) == 0b10000) {
						// Update this once every 16 frames
						electricalUsage = Random.value * 0.95f;
					}
					electricalUsageFlux = Random.value * 0.05f;
				} else {
					flux = -0.025f;
				}
				tubeHeat = Mathf.Clamp01(tubeHeat + flux);
			}

			public void AnimateAsUniformPulse(bool powered, int frame) {
				float flux = 0.0125f;
				if (!powered) {
					flux = -0.0125f;
				}
				electricalUsage = Mathf.Clamp01(electricalUsage + flux);

				frame &= 0x7F;
				float f = (frame / (float)0x7F);
				f *= Mathf.PI * 2.0f;
				f = (Mathf.Sin(f) + 1f) * 0.5f;
				tubeHeat = f * electricalUsage;
			}

			public void AnimateAsRandomPulse(bool powered, int frame) {
				float flux = Random.value * 0.0125f;
				if (!powered) {
					flux *= -1;
				}
				electricalUsage = Mathf.Clamp01(electricalUsage + flux);
				electricalUsageFlux = flux;

				frame &= 0x7F;
				float f = (frame / (float)0x7F);
				f *= Mathf.PI * 2.0f;
				f = (Mathf.Sin(f) + 1f) * 0.5f;
				tubeHeat = (f * electricalUsage) * (flux + 0.9875f);
			}

			public void AnimateAsFlash(bool powered, int frame) {
				if (!powered) {
					electricalUsage = Mathf.Clamp01(electricalUsage - 0.025f);
				} else {
					bool shouldFlash = (frame & 0b00010000) != 0; // I think this will work?
					if (shouldFlash) {
						electricalUsage = Mathf.Clamp01(electricalUsage + 0.05f);
					} else {
						electricalUsage = Mathf.Clamp01(electricalUsage - 0.05f);
					}
				}
				tubeHeat = Mathf.Round(electricalUsage);
			}

			public void AnimateAsConstant(bool powered, int frame) {
				float flux = 0.0125f;
				if (!powered) {
					flux = -0.0125f;
				}
				tubeHeat = Mathf.Clamp01(tubeHeat + flux);
			}

			#endregion


			/// <summary>
			/// Creates a color from the values stored in this data block based on the given skin index.
			/// </summary>
			/// <returns></returns>
			public Color GetColor(SkinType skin) {
				switch (skin) {
					case SkinType.Ember:
						return AsBlackbodyColor();
					case SkinType.Plasma:
						return AsPlasmaColor();
					case SkinType.FakeFuses:
						return AsFuseColor();
					case SkinType.FakeFusesAlt:
						return AsFuseColor2();
					default:
						int v = (int)skin;
						int r = (v & 0x00FF0000) >> 16;
						int g = (v & 0x0000FF00) >>  8;
						int b = (v & 0x000000FF) >>  0;
						float rf = r / 255f;
						float gf = g / 255f;
						float bf = b / 255f;
						return new Color(rf * tubeHeat, gf * tubeHeat, bf * tubeHeat);
				}
			}

			public void Animate(AnimationType animation, bool powered, int frame) {
				switch (animation) {
					default:
					case AnimationType.Ember:
						AnimateAsEmber(powered, frame);
						break;
					case AnimationType.UniformPulse:
						AnimateAsUniformPulse(powered, frame);
						break;
					case AnimationType.RandomPulse:
						AnimateAsRandomPulse(powered, frame);
						break;
					case AnimationType.Flash:
						AnimateAsFlash(powered, frame);
						break;
					case AnimationType.Solid:
						AnimateAsConstant(powered, frame);
						break;
				}
			}
		}

		public enum SkinType {
			/// <summary>
			/// The cells use a blackbody color.
			/// </summary>
			Ember,

			/// <summary>
			/// The cells use a bright cyan-teal mix.
			/// </summary>
			Plasma,

			/// <summary>
			/// The cells use pure RGB blue, mimicing the fuse effect's color set.
			/// </summary>
			FakeFuses,

			/// <summary>
			/// Same as <see cref="FakeFuses"/> but it has a more tealish tint to it.
			/// </summary>
			FakeFusesAlt,

			COUNT

		}

		public enum AnimationType {
			/// <summary>
			/// The fuses fade in and out at somewhat random intervals. The interval is able to change.
			/// </summary>
			Ember,

			/// <summary>
			/// There is a uniform pulsing animation across all nodes at once.
			/// </summary>
			UniformPulse,

			/// <summary>
			/// There is a pulsing animation, but each cell has its own constant timer.
			/// </summary>
			RandomPulse,

			/// <summary>
			/// There is a uniform flashing animation at a constant speed, based on the frame number.
			/// </summary>
			Flash,

			/// <summary>
			/// The effect remains at a solid color, it will still fade in and out when powering on or off.
			/// </summary>
			Solid,

			COUNT
		}

	}
}
