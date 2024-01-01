using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Exceptions;
using XansTools.Utilities.Cecil;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public static partial class MechPlayerWorldInteractions {

		private static void InitializePhysics() {
			Log.LogMessage("Injecting physics related code...");
			IL.Player.Jump += InjectJump;
		}

		private static void InjectJump(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			bool found = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchLdcR4(1f),
				instruction => instruction.MatchLdcR4(1.15f),
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchCall("get_Adrenaline"),
				instruction => instruction.MatchCall("Lerp"),
				instruction => instruction.MatchStloc(0)
			);

			if (!found) throw new RuntimePatchFailureException("Failed to find the jump method's code for jump power!");

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.EmitDelegate<Func<Player, float, float>>((player, jumpPower) => {
				if (MechPlayer.From(player) is MechPlayer mech) {
					if (mech.WouldBeSwimming()) {
						return jumpPower * 2f;
					}
				}
				return jumpPower;
			});
			cursor.Emit(OpCodes.Stloc_0);
		}
	}
}
