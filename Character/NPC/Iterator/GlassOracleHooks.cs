using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.NPC.Iterator.Graphics;
using XansCharacter.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.General;

namespace XansCharacter.Character.NPC.Iterator {

	public static class GlassOracleHooks {

		private static readonly ConditionalWeakTable<Oracle, ShallowGlassOracle> _oracles = new ConditionalWeakTable<Oracle, ShallowGlassOracle>();

		private static bool IsGlass(Oracle oracle) => oracle?.ID == Oracles.GlassID;

		public static ShallowGlassOracle Glass(Oracle oracle) {
			if (IsGlass(oracle)) {
				return _oracles.Get(oracle);
			}
			return null;
		}

		public static void Initialize(AutoPatcher patcher) {
			On.Room.ReadyForAI += OnReadyForAI;
			On.Oracle.ctor += OnOracleConstructing;
			On.Oracle.Update += OnUpdate;
			patcher.InjectIntoProperty<Oracle>(nameof(Oracle.Consious), new HarmonyMethod(typeof(GlassOracleHooks).GetMethod(nameof(GetIsConsious), BindingFlags.NonPublic | BindingFlags.Static)));
		}

		private static bool GetIsConsious(Oracle __instance, ref bool __result) {
			ShallowGlassOracle glass = Glass(__instance);
			if (glass != null) {
				__result = glass.IsConsious();
				return false;
			}
			return true;
		}

		private static void OnUpdate(On.Oracle.orig_Update originalMethod, Oracle @this, bool eu) {
			originalMethod(@this, eu);
			Glass(@this)?.Update(eu);
		}

		private static void OnOracleConstructing(On.Oracle.orig_ctor originalMethod, Oracle @this, AbstractPhysicalObject abstractPhysicalObject, Room room) {
			originalMethod(@this, abstractPhysicalObject, room);
			if (room.oracleWantToSpawn == Oracles.GlassID) {
				_oracles.Add(@this, new ShallowGlassOracle(@this));
			}
		}

		private static void OnReadyForAI(On.Room.orig_ReadyForAI originalMethod, Room @this) {
			originalMethod(@this);
			if (@this.abstractRoom.name == $"{DreamsOfInfiniteGlassPlugin.REGION_PREFIX}_AI") {
				if (@this.world != null && @this.game != null) {
					Log.LogTrace($"I want to spawn glass, the room is {DreamsOfInfiniteGlassPlugin.REGION_PREFIX}_AI.");
					@this.oracleWantToSpawn = Oracles.GlassID;
					try {
						if (@this.abstractRoom == null) {
							Log.LogWarning("But I cannot, because the abstract room is null.");
							return;
						}
						Oracle obj = new Oracle(
							new AbstractPhysicalObject(@this.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(@this.abstractRoom.index, 15, 15, -1), @this.game.GetNewID()), 
							@this
						);
						Log.LogTrace("Construction complete.");
						@this.AddObject(obj);
						@this.waitToEnterAfterFullyLoaded = Math.Max(@this.waitToEnterAfterFullyLoaded, 80);
					} catch (Exception exc) {
						Log.LogError($"Failed to spawn Glass: {exc}");
					}
				}
			}
		}

	}
}
