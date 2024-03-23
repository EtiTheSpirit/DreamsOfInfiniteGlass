using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.General;
using static XansTools.Utilities.Patchers.MethodOfProvider;

namespace DreamsOfInfiniteGlass.Character {

	/// <summary>
	/// The death contextualizer allows performing a harmony modification to any method that calls <see cref="Creature.Die"/>
	/// such that <see cref="MechPlayer.AboutToDie"/> is called just before.
	/// </summary>
	public static class DeathContextualizer {

		/// <summary>
		/// Given a method belonging to something that calls <see cref="Creature.Die"/>, this will edit its code such that
		/// if it kills <see cref="MechPlayer"/> then it will call a special method just beforehand.
		/// </summary>
		/// <param name="harmony"></param>
		/// <param name="target"></param>
		public static void CreateDeathContextIn(Harmony harmony, MethodBinding target) {
			_transpileIntoDieNoSupernova ??= typeof(DeathContextualizer).GetMethod(nameof(TranspileIntoDieHandler), BindingFlags.Static | BindingFlags.NonPublic);
			_transpileIntoDieWithSupernova ??= typeof(DeathContextualizer).GetMethod(nameof(TranspileIntoDieHandlerWithSupernova), BindingFlags.Static | BindingFlags.NonPublic);

			PatchProcessor processor = harmony.CreateProcessor(target.method);
			processor.AddTranspiler(new HarmonyMethod(target.forceSupernova ? _transpileIntoDieWithSupernova : _transpileIntoDieNoSupernova));
			//processor.AddTranspiler(HarmonyMethodOf(TranspileIntoDieHandler));
			processor.Patch();
		}

		private static MethodInfo _transpileIntoDieNoSupernova;
		private static MethodInfo _transpileIntoDieWithSupernova;

		/// <summary>
		/// Given one or more methods belonging to something that calls <see cref="Creature.Die"/>, this will edit its code such that
		/// if it kills <see cref="MechPlayer"/> then it will call a special method just beforehand.
		/// </summary>
		/// <param name="harmony"></param>
		/// <param name="target"></param>
		public static void CreateDeathContextsIn(Harmony harmony, params MethodBinding[] targets) {
			foreach (MethodBinding target in targets) {
				CreateDeathContextIn(harmony, target);
			}
		}

		private static IEnumerable<CodeInstruction> TranspileIntoDieHandler(IEnumerable<CodeInstruction> instructions, MethodBase original) {
			return CommonTranspileProcedure(instructions, original, false);
		}

		private static IEnumerable<CodeInstruction> TranspileIntoDieHandlerWithSupernova(IEnumerable<CodeInstruction> instructions, MethodBase original) {
			return CommonTranspileProcedure(instructions, original, true);
		}

		private static IEnumerable<CodeInstruction> CommonTranspileProcedure(IEnumerable<CodeInstruction> instructions, MethodBase original, bool causeSupernova) {
			int instructionIndex = -1;
			foreach (CodeInstruction instruction in instructions) {
				instructionIndex++;

				if (instruction.opcode == OpCodes.Callvirt) {
					if (instruction.operand is MethodBase method && IsDieMethod(method)) {
						// I know for a fact that the only stack value will be the thing that is about to die, due to the method's signature.

						// Dupe the creature that is being killed, so that we can use it here and then again for the die method call.
						yield return new CodeInstruction(OpCodes.Dup);

						// Now get the type of the caller.
						yield return new CodeInstruction(OpCodes.Ldtoken, original.DeclaringType);
						yield return new CodeInstruction(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle)));
						//yield return new CodeInstruction(OpCodes.Call, methodof(Type.GetTypeFromHandle));

						// And finally load this, or null if the caller is static
						if (original.IsStatic) {
							yield return new CodeInstruction(OpCodes.Ldnull);
						} else {
							yield return new CodeInstruction(OpCodes.Ldarg_0);
						}

						// Now to use all of this:
						// Call TryTellMechPlayerAboutDeath
						yield return new CodeInstruction(causeSupernova ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
						yield return new CodeInstruction(OpCodes.Call, typeof(DeathContextualizer).GetMethod(nameof(TryTellMechPlayerAboutDeath), BindingFlags.Static | BindingFlags.NonPublic));
						//yield return new CodeInstruction(OpCodes.Call, methodof(TryTellMechPlayerAboutDeath));

						// And lastly, return the original Die() call.
						// (Just fall through)
					}
				}
				yield return instruction;
			}
		}

		private static bool IsDieMethod(MethodBase method) {
			if (method.IsStatic) return false;
			if (method.Name != "Die") return false;
			if (method.GetParameters().Length != 0) return false;
			if (!method.DeclaringType.IsAssignableTo(typeof(Creature))) return false;
			return true;
		}

		private static void TryTellMechPlayerAboutDeath(object dyingThing, Type callerType, object callerInstance, bool forceSupernova) {
			if (dyingThing is Creature creature && creature is Player player && MechPlayer.From(player) is MechPlayer mech) {
				mech.AboutToDie(callerType, callerInstance, forceSupernova);
			}
		}

		/// <summary>
		/// A container with both a method to patch and whether or not the death type should result in a supernova.
		/// </summary>
		public readonly struct MethodBinding {

			/// <summary>
			/// The method to patch.
			/// </summary>
			public readonly MethodBase method;

			/// <summary>
			/// If true, calling Die() from this method should cause a supernova.
			/// </summary>
			public readonly bool forceSupernova;

			public static implicit operator MethodBinding(ValueTuple<MethodBase, bool> tuple) => new MethodBinding(tuple.Item1, tuple.Item2);

			public static implicit operator MethodBinding(MethodBase method) => new MethodBinding(method, false);

			public static implicit operator ValueTuple<MethodBase, bool>(MethodBinding @this) => (@this.method, @this.forceSupernova);

			public MethodBinding(MethodBase method, bool forceSupernova) {
				this.method = method;
				this.forceSupernova = forceSupernova;
			}

		}
	}
}
