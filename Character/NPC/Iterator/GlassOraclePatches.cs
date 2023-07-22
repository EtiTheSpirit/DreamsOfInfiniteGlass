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
using XansCharacter.Data.Registry;
using XansTools.Utilities;

namespace XansCharacter.Character.NPC.Iterator {

	public static class GlassOraclePatches {

		public static void Initialize() {
			On.Room.ReadyForAI += OnReadyForAI;
		}

		private static void OnReadyForAI(On.Room.orig_ReadyForAI originalMethod, Room @this) {
			originalMethod(@this);
			if (@this.abstractRoom.name == "16_AI") {
				Log.LogTrace("I want to spawn glass.");
				@this.oracleWantToSpawn = Oracles.GLASS_ORACLE_ID;
				try {
					if (@this.abstractRoom == null) {
						Log.LogWarning("But I cannot, because the abstract room is null.");
						return;
					}
					GlassOracle obj = new GlassOracle(new AbstractPhysicalObject(@this.world, AbstractPhysicalObject.AbstractObjectType.Oracle, null, new WorldCoordinate(@this.abstractRoom.index, 15, 15, -1), @this.game.GetNewID()), @this, new Vector2(500, 360));
					Log.LogTrace("Construction complete.");
					@this.AddObject(obj);
					@this.waitToEnterAfterFullyLoaded = Math.Max(@this.waitToEnterAfterFullyLoaded, 80);
				} catch (Exception exc) { 
					Log.LogError($"Failed to spawn Glass: {exc}");
				}
			} else {
				Log.LogTrace($"I can't spawn glass in {@this.abstractRoom.name}");
			}
		}

	}
}
