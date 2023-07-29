using HarmonyLib;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.LoadedAssets;
using XansTools.Utilities;

namespace XansCharacter.Character.NPC.Iterator.Graphics {
	public static class GlassOracleGraphics_ShadowHooks {

		public static void MakeShadowHooks(AutoPatcher patcher) {
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

			On.OracleGraphics.Gown.Color += GetGownColor;
			On.OracleGraphics.Halo.DrawSprites += OnDrawHaloSprites;
			On.OracleGraphics.Halo.InitiateSprites += OnBuildHaloSprites;
			On.OracleGraphics.Halo.Update += OnHaloUpdating;

			patcher.InjectIntoProperty<LightSource>(nameof(LightSource.LayerName), new HarmonyMethod(typeof(GlassOracleGraphics_ShadowHooks).GetMethod(nameof(GetNewLayerName), BindingFlags.Static | BindingFlags.NonPublic)));
			// On.LightSource.AddToContainer += OnAddingLightToContainer;
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

			if (@this.owner.oracle is GlassOracle) {
				rCam.room.lightAngle = Vector2.zero;
				int index;
				const int NUM_VECTOR_CIRCLES = 2;

				for (int i = 0; i < NUM_VECTOR_CIRCLES; i++) {
					// VECTOR CIRCLES
					index = @this.firstSprite + i;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GlassOracleGraphics.GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.AdditiveVertexColoredVectorCircle;
				}
				for (int i = 0; i < @this.connections.Length; i++) {
					// MYCELIA
					index = @this.firstSprite + i + NUM_VECTOR_CIRCLES;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GlassOracleGraphics.GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.AdditiveVertexColored;
				}

				index = @this.firstBitSprite;
				for (int bitGroupIndex = 0; bitGroupIndex < @this.bits.Length; bitGroupIndex++) {
					// FILLED CHUNKS AROUND HALO
					OracleGraphics.Halo.MemoryBit[] bits = @this.bits[bitGroupIndex];
					for (int bitIndex = 0; bitIndex < bits.Length; bitIndex++) {
						FSprite sprite = sLeaser.sprites[index++];
						sprite.color = GlassOracleGraphics.GLASS_SPARK_COLOR_IDLE;
						sprite.shader = XansAssets.AdditiveVertexColored;
					}
				}
			}
		}

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

		private static void OnDrawHaloSprites(On.OracleGraphics.Halo.orig_DrawSprites originalMethod, OracleGraphics.Halo @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
			if (@this.owner.oracle is GlassOracle) {
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

		private static Color GetGownColor(On.OracleGraphics.Gown.orig_Color originalMethod, OracleGraphics.Gown @this, float f) {
			if (@this.owner.oracle is GlassOracle) {
				return Color.Lerp(GlassOracleGraphics.GLASS_ROBE_COLOR_BASE, GlassOracleGraphics.GLASS_ROBE_COLOR_HIGHLIGHT, f);
			}
			return originalMethod(@this, f);
		} 
	}
}
