﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Configs;
using XansCharacter.LoadedAssets;

namespace XansCharacter.Data.World {
	public static class WorldShaderMarshaller {

		internal static void Initialize() {
			Log.LogMessage("Initializing world shader marshaller...");
			On.RoomCamera.MoveCamera_Room_int += OnMoveCamera;
		}

		private static void OnMoveCamera(On.RoomCamera.orig_MoveCamera_Room_int originalMethod, RoomCamera @this, Room newRoom, int camPos) {
			originalMethod(@this, newRoom, camPos);

			Dictionary<string, bool> macros = GetDefaultMacros(@this, newRoom);
			LoadIndividualMacros(@this, newRoom, macros);

			if (Configuration.AdvancedShaderSystems) {
				Log.LogTrace($"Advanced shader systems are enabled. Sunlight-related static branches are now available.");
				Shader.EnableKeyword("SUNLIGHT_STATIC_BRANCHES_AVAILABLE");
			} else {
				Log.LogTrace($"Advanced shader systems are disabled. Sunlight-related static branches are no longer available.");
				Shader.DisableKeyword("SUNLIGHT_STATIC_BRANCHES_AVAILABLE");
			}
		}

		private static Dictionary<string, bool> GetDefaultMacros(RoomCamera camera, Room room) {
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
							camera.levelGraphic.shader = XansAssets.SpecializedLevelShader;
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