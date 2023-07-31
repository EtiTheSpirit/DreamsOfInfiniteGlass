using HarmonyLib;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Data.Registry;
using XansCharacter.LoadedAssets;
using XansTools.Utilities;
using XansTools.Utilities.General;

namespace XansCharacter.Character.NPC.Iterator.Graphics {
	public static class GlassOracleGraphicsHooks {

		private static readonly ConditionalWeakTable<OracleGraphics, ShallowGlassOracleGraphics> _graphics = new ConditionalWeakTable<OracleGraphics, ShallowGlassOracleGraphics>();

		/// <summary>
		/// The base (darker) color of Glass's robes. This is the color used around the chest area.
		/// </summary>
		public static Color GLASS_ROBE_COLOR_BASE => new Color(1.000f, 0.470f, 0.100f);

		/// <summary>
		/// The highlight (brighter) color of Glass's robes. This is the color used at the end of the sleeves and at the bottom of the gown.
		/// </summary>
		public static Color GLASS_ROBE_COLOR_HIGHLIGHT => new Color(1.000f, 0.650f, 0.420f);

		/// <summary>
		/// The color of Glass's sparks/halo whilst idle.
		/// </summary>
		public static Color GLASS_SPARK_COLOR_IDLE => _sparkColorOverride1;
		private static readonly Color _sparkColorOverride1 = new Color(166f/385f, 224/385f, 213/385f, 0.7f).AlphaAsIntensity();

		private static bool IsGlass(OracleGraphics gfx) => gfx.oracle.ID == Oracles.GlassID;

		private static ShallowGlassOracleGraphics GlassGraphics(OracleGraphics @this) {
			if (IsGlass(@this)) {
				return _graphics.Get(@this);
			}
			return null;
		}

		public static void Initialize(AutoPatcher patcher) {
			/*
			patcher.TurnShadowedMethodIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.HandSprite));
			patcher.TurnShadowedMethodIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.FootSprite));
			patcher.TurnShadowedMethodIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.PhoneSprite));
			patcher.TurnShadowedMethodIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.EyeSprite));

			patcher.TurnShadowedPropertyIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.HeadSprite));
			patcher.TurnShadowedPropertyIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.ChinSprite));
			patcher.TurnShadowedPropertyIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.MoonThirdEyeSprite));
			patcher.TurnShadowedPropertyIntoOverride<OracleGraphics, GlassOracleGraphics>(nameof(OracleGraphics.MoonSigilSprite));
			*/

			On.OracleGraphics.ctor += OnConstructingGraphics;

			On.OracleGraphics.Gown.Color += GetGownColor;
			On.OracleGraphics.Halo.DrawSprites += OnDrawHaloSprites;
			On.OracleGraphics.Halo.InitiateSprites += OnBuildHaloSprites;
			On.OracleGraphics.Halo.Update += OnHaloUpdating;

			On.OracleGraphics.FootSprite += OnGettingFootSprite;
			On.OracleGraphics.HandSprite += OnGettingHandSprite;
			On.OracleGraphics.PhoneSprite += OnGettingPhoneSprite;
			On.OracleGraphics.EyeSprite += OnGettingEyeSprite;

			On.OracleGraphics.InitiateSprites += OnInitiatingSprites;
			On.OracleGraphics.DrawSprites += OnDrawingSprites;
			On.OracleGraphics.Update += OnUpdate;

			On.OracleGraphics.AddToContainer += OnAddingToContainer;
			On.OracleGraphics.ApplyPalette += OnApplyingPalette;

			patcher.InjectIntoProperty<LightSource>(nameof(LightSource.LayerName), new HarmonyMethod(typeof(GlassOracleGraphicsHooks).GetMethod(nameof(GetNewLayerName), BindingFlags.Static | BindingFlags.NonPublic)));
			patcher.InjectIntoProperty<OracleGraphics>(nameof(OracleGraphics.HeadSprite), new HarmonyMethod(typeof(GlassOracleGraphicsHooks).GetMethod(nameof(GetHeadSprite), BindingFlags.Static | BindingFlags.NonPublic)));
			patcher.InjectIntoProperty<OracleGraphics>(nameof(OracleGraphics.ChinSprite), new HarmonyMethod(typeof(GlassOracleGraphicsHooks).GetMethod(nameof(GetChinSprite), BindingFlags.Static | BindingFlags.NonPublic)));
			//patcher.InjectIntoProperty<OracleGraphics>(nameof(OracleGraphics.MoonSigilSprite), new HarmonyMethod(typeof(GlassOracleGraphicsHooks).GetMethod(nameof(GetSigilSprite), BindingFlags.Static | BindingFlags.NonPublic)));
			//patcher.InjectIntoProperty<OracleGraphics>(nameof(OracleGraphics.MoonThirdEyeSprite), new HarmonyMethod(typeof(GlassOracleGraphicsHooks).GetMethod(nameof(GetThirdEyeSprite), BindingFlags.Static | BindingFlags.NonPublic)));
		}

		private static void OnApplyingPalette(On.OracleGraphics.orig_ApplyPalette originalMethod, OracleGraphics @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(@this);
			if (gfx != null) gfx.ApplyPalette(sLeaser, rCam, palette);
			else originalMethod(@this, sLeaser, rCam, palette);
		}

		private static void OnAddingToContainer(On.OracleGraphics.orig_AddToContainer originalMethod, OracleGraphics @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(@this);
			if (gfx != null) gfx.AddToContainer(sLeaser, rCam, newContainer);
			else originalMethod(@this, sLeaser, rCam, newContainer);
		}

		private static void OnUpdate(On.OracleGraphics.orig_Update originalMethod, OracleGraphics @this) {
			originalMethod(@this); // ALWAYS CALL.
			GlassGraphics(@this)?.Update();
		}

		private static void OnDrawingSprites(On.OracleGraphics.orig_DrawSprites originalMethod, OracleGraphics @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(@this);
			if (gfx != null) gfx.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			else originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
		}

		private static void OnInitiatingSprites(On.OracleGraphics.orig_InitiateSprites originalMethod, OracleGraphics @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(@this);
			if (gfx != null) gfx.InitiateSprites(sLeaser, rCam);
			else originalMethod(@this, sLeaser, rCam);
		}

		private static int OnGettingEyeSprite(On.OracleGraphics.orig_EyeSprite originalMethod, OracleGraphics @this, int e) {
			return GlassGraphics(@this)?.EyeSprite(e) ?? originalMethod(@this, e);
		}

		private static int OnGettingPhoneSprite(On.OracleGraphics.orig_PhoneSprite originalMethod, OracleGraphics @this, int side, int part) {
			return GlassGraphics(@this)?.PhoneSprite(side, part) ?? originalMethod(@this, side, part);
		}

		private static int OnGettingHandSprite(On.OracleGraphics.orig_HandSprite originalMethod, OracleGraphics @this, int side, int part) {
			return GlassGraphics(@this)?.HandSprite(side, part) ?? originalMethod(@this, side, part);
		}

		private static int OnGettingFootSprite(On.OracleGraphics.orig_FootSprite originalMethod, OracleGraphics @this, int side, int part) {
			return GlassGraphics(@this)?.FootSprite(side, part) ?? originalMethod(@this, side, part);
		}

		private static void OnConstructingGraphics(On.OracleGraphics.orig_ctor originalMethod, OracleGraphics @this, PhysicalObject ow) {
			if (IsGlass(@this)) {
				_graphics.Add(@this, new ShallowGlassOracleGraphics(@this, ow));
			} else {
				originalMethod(@this, ow);
			}
		}

		private static bool GetNewLayerName(LightSource __instance, ref string __result) {
			if (__instance.room.abstractRoom.name == $"{DreamsOfInfiniteGlassPlugin.REGION_PREFIX}_AI") {
				__result = "Foreground"; // Override the light layer.
				
				// Normally it goes to ForegroundLights or Water
				// This override is performed specifically for this one room because of the custom shaders used on Glass's halo.
				// Due to how they operate (and more, due to the fact that they are translucent), they would falsely cast a shadow
				// which can be prevented by pulling the light down to the same layer they exist on.
				Log.LogTrace($"A light was changed to the Foreground layer due to being in {DreamsOfInfiniteGlassPlugin.REGION_PREFIX}_AI.");
				return false;
			}
			return true;
		}

		private static bool GetHeadSprite(OracleGraphics __instance, ref int __result) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(__instance);
			if (gfx != null) {
				__result = gfx.HeadSprite;
				return false;
			}
			return true;
		}
		private static bool GetChinSprite(OracleGraphics __instance, ref int __result) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(__instance);
			if (gfx != null) {
				__result = gfx.ChinSprite;
				return false;
			}
			return true;
		}

		/*
		private static bool GetSigilSprite(OracleGraphics __instance, ref int __result) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(__instance);
			if (gfx != null) {
				__result = gfx.SigilSprite;
				return false;
			}
			return true;
		}
		private static bool GetThirdEyeSprite(OracleGraphics __instance, ref int __result) {
			ShallowGlassOracleGraphics gfx = GlassGraphics(__instance);
			if (gfx != null) {
				__result = gfx.ThirdEyeSprite;
				return false;
			}
			return true;
		}
		*/

		#region Halo

		private static void OnHaloUpdating(On.OracleGraphics.Halo.orig_Update originalMethod, OracleGraphics.Halo @this) {
			GlassOracleBehavior behavior = @this.owner.oracle.oracleBehavior as GlassOracleBehavior;
			if (behavior != null) {
				if (behavior.CurrentConnectionActivity >= 0) {
					@this.connectionsFireChance = behavior.CurrentConnectionActivity;
				}
			}
			originalMethod(@this);
		}

		private static void OnBuildHaloSprites(On.OracleGraphics.Halo.orig_InitiateSprites originalMethod, OracleGraphics.Halo @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			originalMethod(@this, sLeaser, rCam);

			if (@this.owner.oracle.ID == Oracles.GlassID) {
				rCam.room.lightAngle = Vector2.zero;
				int index;
				const int NUM_VECTOR_CIRCLES = 2;

				for (int i = 0; i < NUM_VECTOR_CIRCLES; i++) {
					// VECTOR CIRCLES
					index = @this.firstSprite + i;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.Shaders.AdditiveVertexColoredVectorCircle;
				}
				for (int i = 0; i < @this.connections.Length; i++) {
					// MYCELIA
					index = @this.firstSprite + i + NUM_VECTOR_CIRCLES;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.Shaders.AdditiveVertexColored;
				}

				index = @this.firstBitSprite;
				for (int bitGroupIndex = 0; bitGroupIndex < @this.bits.Length; bitGroupIndex++) {
					// FILLED CHUNKS AROUND HALO
					OracleGraphics.Halo.MemoryBit[] bits = @this.bits[bitGroupIndex];
					for (int bitIndex = 0; bitIndex < bits.Length; bitIndex++) {
						FSprite sprite = sLeaser.sprites[index++];
						sprite.color = GLASS_SPARK_COLOR_IDLE;
						sprite.shader = XansAssets.Shaders.AdditiveVertexColored;
					}
				}
			}
		}
		private static void OnDrawHaloSprites(On.OracleGraphics.Halo.orig_DrawSprites originalMethod, OracleGraphics.Halo @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
			if (@this.owner.oracle.ID == Oracles.GlassID) {
				int spriteIndex;

				for (int connectionIndex = 0; connectionIndex < @this.connections.Length; connectionIndex++) {
					// MYCELIA
					spriteIndex = @this.firstSprite + connectionIndex + 2;
					FSprite sprite = sLeaser.sprites[spriteIndex];
					OracleGraphics.Halo.Connection connection = @this.connections[connectionIndex];
					if (connection.lightUp > 0.05f) {
						ClampVerticesIntoChamber(sprite, @this.owner.oracle.arm, camPos);
					}
				}

				for (int connectionIndex = 0; connectionIndex < @this.connections.Length; connectionIndex++) {
					// MYCELIA
					spriteIndex = @this.firstSprite + connectionIndex + 2;
					FSprite sprite = sLeaser.sprites[spriteIndex];
					OracleGraphics.Halo.Connection connection = @this.connections[connectionIndex];
					//sprite.color = new Color(0, 0, 0); // GlassOracleGraphics.GLASS_SPARK_COLOR_OVERRIDE;
					sprite.alpha = connection.lightUp <= 0.05f ? 0f : 1f; // This is okay, alpha controls its brightness.
				}

				/*
				spriteIndex = @this.firstBitSprite;
				for (int bitGroupIndex = 0; bitGroupIndex < @this.bits.Length; bitGroupIndex++) {
					// FILLED CHUNKS AROUND HALO
					OracleGraphics.Halo.MemoryBit[] bits = @this.bits[bitGroupIndex];
					for (int bitIndex = 0; bitIndex < bits.Length; bitIndex++) {
						FSprite sprite = sLeaser.sprites[spriteIndex++];
						//sprite.color = GlassOracleGraphics.GLASS_SPARK_COLOR_OVERRIDE;
						//sprite.shader = XansAssets.XAdditiveVtxClrNA;
						sprite.alpha = sin;
					}
				}
				*/
			}
		}

		#endregion

		#region Utils

		private static void ClampVerticesIntoChamber(Vector2[] vertices, Vector2 min, Vector2 max, Vector2 camPos) {
			float minX = (min.x - camPos.x) - 2;
			float maxX = (max.x - camPos.x) + 2;
			float minY = (min.y - camPos.y) - 2;
			float maxY = (max.y - camPos.y) + 2;

			for (int i = 0; i < vertices.Length; i++) {
				vertices[i] = new Vector2(Mathf.Clamp(vertices[i].x, minX, maxX), Mathf.Clamp(vertices[i].y, minY, maxY));
			}
		}

		private static void ClampVerticesIntoChamber(FSprite sprite, Oracle.OracleArm arm, Vector2 camPos) {
			if (sprite is TriangleMesh triangleMesh) {
				Vector2 min = arm.cornerPositions[3];
				Vector2 max = arm.cornerPositions[1];
				ClampVerticesIntoChamber(triangleMesh.vertices, min, max, camPos);
			}
		}

		#endregion

		#region Gown

		private static Color GetGownColor(On.OracleGraphics.Gown.orig_Color originalMethod, OracleGraphics.Gown @this, float f) {
			if (@this.owner.oracle.ID == Oracles.GlassID) {
				return Color.Lerp(GLASS_ROBE_COLOR_BASE, GLASS_ROBE_COLOR_HIGHLIGHT, f);
			}
			return originalMethod(@this, f);
		}

		#endregion

	}
}
