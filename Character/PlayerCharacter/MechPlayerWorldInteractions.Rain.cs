using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.RW;
using XansTools.Utilities.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil;
using XansTools.Exceptions;
using UnityEngine;
using MonoMod.RuntimeDetour;
using static XansTools.Utilities.Patchers.MethodOfProvider;
using System.Reflection;
using DreamsOfInfiniteGlass.Configs;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public static partial class MechPlayerWorldInteractions {

		private static void InitializeRain() {
			Log.LogMessage("Hooking rain-related stuff...");
			On.GlobalRain.DeathRain.DeathRainUpdate += OnUpdatingDeathRain;
			On.RoomRain.RoomRainFloodShake += OnFloodShake;
			IL.RoomRain.ThrowAroundObjects += InjectRainThrowObjects;
			On.RoomRain.ThrowAroundObjects += OnThrowingAroundObjects;
			On.RoomRain.DrawSprites += OnDrawingRain;
			Hook hook = new Hook(
				typeof(RoomRain).GetProperty(nameof(RoomRain.FloodLevel)).GetMethod,
				typeof(MechPlayerWorldInteractions).GetMethod(nameof(GetFloodLevel), BindingFlags.Static | BindingFlags.NonPublic)
			);
			hook.Apply();
		}

		private static void OnThrowingAroundObjects(On.RoomRain.orig_ThrowAroundObjects originalMethod, RoomRain @this) {
			if (Configuration.DisableRoomRainThrowing && WorldTools.GetPlayers().Any(plr => MechPlayer.From(plr) is not null)) return;
			originalMethod(@this);
		}

		private static float GetFloodLevel(Func<RoomRain, float> originalMethod, RoomRain @this) {
			float org = originalMethod(@this);
			return org;
		}

		private static void OnDrawingRain(On.RoomRain.orig_DrawSprites originalMethod, RoomRain @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos) {
			originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
			if (WorldTools.GetPlayers().Any(plr => MechPlayer.From(plr) is not null)) {
				// There is a mech player in this instance. In this scenario, be it singleplayer or co-op,
				// draw death rain with only part of its intensity.
				float storedRainEverywhere = Shader.GetGlobalFloat("_rainEverywhere");
				float storedRainIntensity = Shader.GetGlobalFloat("_rainIntensity");
				storedRainEverywhere *= 0.6f;
				storedRainIntensity *= 0.875f;
				Shader.SetGlobalFloat("_rainEverywhere", storedRainEverywhere);
				Shader.SetGlobalFloat("_rainIntensity", storedRainIntensity);
			}
		}

		private static void InjectRainThrowObjects(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			
			bool foundStartOfLoop = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchCallvirt("get_Item"),
				instruction => instruction.MatchCallvirt("get_bodyChunks"),
				instruction => instruction.MatchLdloc(2),
				instruction => instruction.MatchLdelemRef(),
				instruction => instruction.MatchStloc(3)
			);

			if (!foundStartOfLoop) throw new RuntimePatchFailureException(nameof(RoomRain.ThrowAroundObjects), "Failed to find the code that moves and kills creatures during the rain. This is a core mechanic and modifying it is required.");

			// So what I have found is the beginning of the first part of the inner-most loop's code.
			// What I want to do is skip if that body chunk meets a certain condition (read: belongs to the mech player)
			Instruction next = cursor.Next;
			ILLabel carryOn = cursor.MarkLabel();
			cursor.Next = next;
			cursor.Emit(OpCodes.Ldloc_3);
			cursor.Emit(OpCodes.Ldloca_S, (byte)2);
			cursor.EmitDelegate<CheckForMechPlayer>((BodyChunk bodyChunk, ref int iter) => {
				if (bodyChunk.owner is Player player && MechPlayer.From(player) is not null) {
					iter++;
					return false; // Not allowed to continue.
				}
				return true;
			});
			cursor.Emit(OpCodes.Brtrue_S, carryOn);

			// Now the continue statement:
			// Remember where it is.
			Instruction index = cursor.Next;

			// Now find the end of the loop block, where it looks for length and compares it.
			bool foundEndOfLoop = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchCall("op_Multiply"),
				instruction => instruction.MatchCall("op_Addition"),
				instruction => instruction.MatchStfld("vel"),
				instruction => instruction.MatchLdloc(2)
			);

			if (!foundEndOfLoop) throw new RuntimePatchFailureException(nameof(RoomRain.ThrowAroundObjects), "Failed to find the code that continues the loop through all objects in the room.");
			cursor.Index--;
			ILLabel checkLoop = cursor.MarkLabel();

			cursor.Next = index; // Now go back
			cursor.Emit(OpCodes.Br, checkLoop);
		}

		private delegate bool CheckForMechPlayer(BodyChunk chunk, ref int iteration);

		private static float OnFloodShake(On.RoomRain.orig_RoomRainFloodShake originalMethod, Room room, float globalFloodLevel) {
			float shake = originalMethod(room, globalFloodLevel);
			if (WorldTools.GetPlayers().Any(plr => MechPlayer.From(plr) != null)) {
				return 0;
			}
			return shake;
		}

		private static void OnUpdatingDeathRain(On.GlobalRain.DeathRain.orig_DeathRainUpdate originalMethod, GlobalRain.DeathRain @this) {
			originalMethod(@this);

			// Disable the screen shake effect if anyone is SOLSTICE as it is immune to the rain.
			if (WorldTools.GetPlayers().Any(plr => MechPlayer.From(plr) != null)) {
				@this.globalRain.ScreenShake = 0;
				@this.globalRain.MicroScreenShake = 0;
			}
		}


	}
}
