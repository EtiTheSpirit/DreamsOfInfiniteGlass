#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Configs;
using DreamsOfInfiniteGlass.LoadedAssets;

namespace DreamsOfInfiniteGlass.Data.World {
	public static class WorldShaderMarshaller {

		// TO FUTURE XAN, TODO:
		// Right now, this technique causes SBCameraScroll to break (as it relies on its own level shader).
		// You need to find time to contact its creator. You need to ask them to add a token-based switch
		// where mods can specify that they don't want the mod to be used for an arbitrary duration or
		// scenario.

		internal static void Initialize() {
			Log.LogMessage("Initializing world shader marshaller...");
			On.RoomCamera.MoveCamera_Room_int += OnMoveCamera;
		}

		private static void OnMoveCamera(On.RoomCamera.orig_MoveCamera_Room_int originalMethod, RoomCamera @this, Room newRoom, int camPos) {
			originalMethod(@this, newRoom, camPos);

			Dictionary<string, bool> macros = GetDefaultMacros(newRoom);
			LoadIndividualMacros(@this, newRoom, macros);

			Shader.EnableKeyword("SUNLIGHT_STATIC_BRANCHES_AVAILABLE");			
		}

		private static Dictionary<string, bool> GetDefaultMacros(Room room) {
			Dictionary<string, bool> macros = new Dictionary<string, bool>();
			if (room == null) return macros;
			if (room.abstractRoom == null) return macros;
			if (room.abstractRoom.world == null) return macros;
			if (room.abstractRoom.world.region == null) return macros;
			if (room.abstractRoom.world.region.name == null) return macros;
			try {
				string macrosPath = AssetManager.ResolveFilePath(Path.Combine("world", room.abstractRoom.world.region.name, "keywords_template.txt"));
				if (File.Exists(macrosPath)) {
					Log.LogTrace($"Loading shader keywords for room {room.abstractRoom.name}...");
					string[] lines = File.ReadAllLines(macrosPath);
					foreach (string line in lines) {
						if (line.StartsWith("#define ")) {
							string macro = line.Substring(8);
							macros[macro] = true;
							Log.LogDebug($"Default keyword {macro}: ENABLED");
						} else if (line.StartsWith("#undef ")) {
							string macro = line.Substring(7);
							macros[macro] = false;
							Log.LogDebug($"Default keyword {macro}: DISABLED");
						}
					}
				}
				return macros;
			} catch (Exception exc) {
				Log.LogError(exc); 
				throw;
			}
		}

		private static void LoadIndividualMacros(RoomCamera camera, Room room, Dictionary<string, bool> macros) {
			if (room == null) return;
			if (room.abstractRoom == null) return;
			if (room.abstractRoom.world == null) return;
			if (room.abstractRoom.world.region == null) return;
			if (room.abstractRoom.world.region.name == null) return;
			if (camera.levelGraphic == null) return;
			try {
				string macrosPath = WorldLoader.FindRoomFile(room.abstractRoom.name, false, "_keywords.txt");
				if (File.Exists(macrosPath)) {
					Log.LogTrace($"Loading shader keywords for room {room.abstractRoom.name}...");
					string[] lines = File.ReadAllLines(macrosPath);
					foreach (string line in lines) {
						if (line.StartsWith("#define ")) {
							string macro = line.Substring(8);
							macros[macro] = true;
						} else if (line.StartsWith("#undef ")) {
							string macro = line.Substring(7);
							macros[macro] = false;
						}
					}
				}

				foreach (KeyValuePair<string, bool> data in macros) {
					if (data.Key == "SUNLIGHT_STATIC_BRANCHES_AVAILABLE") throw new InvalidOperationException("Macro SUNLIGHT_STATIC_BRANCHES_AVAILABLE is a user setting. Do not set it in the level settings.");
					if (data.Key == "SUNLIGHT_EFFECTIVELY_ON") throw new InvalidOperationException("Macro SUNLIGHT_EFFECTIVELY_ON is internal. Do not set it manually. Instead, set SUNLIGHT_RENDERING_ON");
					if (data.Key == "USE_GLASS_LEVEL_SHADER") {
						if (data.Value) {
							Log.LogDebug("Swapping to specialized level shader.");
							camera.levelGraphic.shader = XansAssets.Shaders.SpecializedLevelShader;
						} else {
							Log.LogDebug("Using standard level shader.");
							// Do nothing, let the vanilla code swap it out.
							// @this.levelGraphic.shader = XansAssets.SpecializedLevelShader;
						}
					} else {
						if (data.Value) { 
							Shader.EnableKeyword(data.Key);
							Log.LogDebug($"Keyword {data.Key}: ENABLED");
						} else {
							Shader.DisableKeyword(data.Key);
							Log.LogDebug($"Keyword {data.Key}: DISABLED");
						}
					}
				}
			} catch (Exception exc) {
				Log.LogError(exc);
				throw;
			}
		}

	}
}
