using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Character.PlayerCharacter;
using XansTools.Utilities.Cecil;

namespace XansCharacter.WorldObjects.Injections {
	public static class ZapCoilContextualizer {

		internal static void Initialize() {
			Log.LogTrace("Adding Context to ZapCoil's call to Die()...");
			IL.ZapCoil.Update += InjectZapCoil;
		}

		private static void InjectZapCoil(ILContext il) {
			ILCursor cursor = new ILCursor(il);

			bool foundInstruction = cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchCallvirt("Die"));
			if (!foundInstruction) {
				throw new InvalidOperationException("Failed to find Die method in ZapCoil.Update!");
			}

			Func<Creature, Creature> onZapCoilKilling = OnZapCoilKilling;
			cursor.EmitDelegate(onZapCoilKilling);
		}

		private static Creature OnZapCoilKilling(Creature creature) {
			if (creature is Player player && Extensible.Player.Binder<MechPlayer>.TryGetBinding(player, out WeakReference<MechPlayer> mechRef) && mechRef.TryGetTarget(out MechPlayer mech)) {
				Log.LogTrace("Mech will collapse!");
				mech.Die(true);
			} else {
				Log.LogTrace($"Creature was not the mech player (got: {creature}, WeakReference<MechPlayer> may not have resolved.)");
			}
			return creature; // Return it so that it remains on the stack.
		}
	}
}
