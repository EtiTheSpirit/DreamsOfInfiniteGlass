using DreamsOfInfiniteGlass.Data.Registry;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities.Cecil;

namespace DreamsOfInfiniteGlass.Character.NPC.Iterator {
	public static class GlassOracleArm {

		internal static void Initialize() {
			On.Oracle.OracleArm.Update += OnOracleArmUpdate;
			IL.Oracle.OracleArm.Update += PatchOracleArmUpdate;
		}

		private static void OnOracleArmUpdate(On.Oracle.OracleArm.orig_Update originalMethod, Oracle.OracleArm @this) {
			
			if (@this.oracle.ID == Oracles.GlassID) {
				if (@this.baseMoveSoundLoop == null) {
					@this.baseMoveSoundLoop = new StaticSoundLoop(SoundID.SS_AI_Base_Move_LOOP, @this.oracle.firstChunk.pos, @this.oracle.room, 1f, 1f);
				}
			}
			originalMethod(@this);
		}
		
		private static void PatchOracleArmUpdate(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			ILLabel ifOK = null;
			bool found = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchLdfld("oracle"),
				instruction => instruction.MatchLdfld("ID"),
				instruction => instruction.MatchLdsfld("SS"),
				instruction => instruction.MatchCall("op_Equality"),
				instruction => instruction.MatchBrtrue(out ifOK)
			);
			if (!found) {
				throw new InvalidOperationException("Failed to find the branch in OracleArm.update to control the sound.");
			}
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<Func<Oracle.OracleArm, bool>>(IsGlass);
			cursor.Emit(OpCodes.Brtrue, ifOK);

			cursor.Index -= 10;
			for (int i = 0; i < 20; i++) {
				Log.LogMessage(cursor.Instrs[cursor.Index++].ToStringFixed());
			}
		}

		private static bool IsGlass(Oracle.OracleArm @this) {
			return @this.oracle != null && @this.oracle.ID == Oracles.GlassID;
		}
	}
}
