using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.Cecil;
using XansTools.Utilities.General;
using XansTools.Utilities.RW;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public static partial class MechPlayerWorldInteractions {

		internal static void Initialize() {
			IL.BigEel.JawsSnap += InjectLeviathanKill;
			//IL.ZapCoil.Update += InjectZapCoil;

			DeathContextualizer.CreateDeathContextsIn(
				DreamsOfInfiniteGlassPlugin.Harmony, 
				NETExtensions.QuickGetMethod<ZapCoil>(nameof(ZapCoil.Update)),
				NETExtensions.QuickGetMethod<Spear>(nameof(Spear.HitSomething)),
				NETExtensions.QuickGetMethod<SingularityBomb>(nameof(SingularityBomb.Explode))
			);

			InitializeRain();
			InitializePhysics();
		}

		
		private static void InjectLeviathanKill(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			bool foundLeviathanKill = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchLdelemRef(),
				instruction => instruction.MatchLdloc(7),
				instruction => instruction.MatchCallvirt("get_Item"),
				instruction => instruction.MatchIsinst<Creature>(), 
				// n.b. no jump after this is correct, "isinst" is the "as" keyword
				// and the type was checked in IL above this block.
				instruction => instruction.MatchCallvirt("Die")
			);
			if (!foundLeviathanKill) {
				Log.LogError("Failed to find the leviathan bite kill code! This will break a core feature of the mod, but will not prevent the game from working.");
				return;
			}

			cursor.GotoPrev();
			//cursor.Emit(OpCodes.Ldloc_S, new VariableIndex(7));
			//cursor.Emit(OpCodes.Ldloc_S, (byte)7);
			//cursor.Emit(OpCodes.Isinst, typeof(Creature));
			cursor.Emit(OpCodes.Dup);
			cursor.EmitDelegate<Action<Creature>>((Creature creature) => {
				if (creature is Player player) {
					if (Extensible.Player.Binder<MechPlayer>.TryGetBinding(player, out MechPlayer mech)) {
						mech.Die(true);
					}
				}
			});

			cursor.DumpToLog(str => Log.LogTrace(str));
		}

		private static void InjectZapCoil(ILContext il) {
			ILCursor cursor = new ILCursor(il);

			bool foundInstruction = cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchCallvirt("Die"));
			if (!foundInstruction) {
				throw new InvalidOperationException("Failed to find Die method in ZapCoil.Update!");
			}

			cursor.GotoPrev();
			cursor.EmitDelegate<Func<Creature, Creature>>((Creature creature) => {
				if (creature is Player player && Extensible.Player.Binder<MechPlayer>.TryGetBinding(player, out MechPlayer mech)) {
					Log.LogTrace("Mech will collapse!");
					mech.Die(true);
				} else {
					Log.LogTrace($"Creature was not the mech player (got: {creature}, WeakReference<MechPlayer> may not have resolved.)");
				}
				return creature; // Return it so that it remains on the stack.
			});
		}
	}
}
