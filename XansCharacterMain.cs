using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character;
using XansCharacter.Character.NPC;
using XansCharacter.Character.NPC.Iterator;
using XansCharacter.Character.NPC.Iterator.Graphics;
using XansCharacter.Character.NPC.Iterator.Interaction;
using XansCharacter.Character.PlayerCharacter;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.Data.Players;
using XansCharacter.Data.Registry;
using XansCharacter.LoadedAssets;
using XansTools.Utilities;
using XansCharacter.WorldObjects;
using XansCharacter.Configs;
using XansTools.Utilities.ModInit;
using XansCharacter.Data;

namespace XansCharacter {

	[BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
	[BepInDependency(XansTools.Plugin.PLUGIN_ID, BepInDependency.DependencyFlags.HardDependency)] // XansTools
	[BepInDependency("rwmodding.coreorg.rk", BepInDependency.DependencyFlags.HardDependency)] // RegionKit
	[BepInDependency("rwmodding.coreorg.pom", BepInDependency.DependencyFlags.HardDependency)] // POM
	public class XansCharacterMain : BaseUnityPlugin {

		public const string PLUGIN_NAME = "Dreams of Infinite Glass";
		public const string PLUGIN_ID = "xan.dreamsofinfiniteglass";
		public const string PLUGIN_VERSION = "1.0.0";

		private RemixConfigScreen _cfgScr;
		private ErrorReporter _reporter;
		private AutoPatcher _patcher;

		/// <summary>
		/// Disable optimization and inlining because this needs to call everything it explicitly goes out of its way to call (mostly static initializers that are empty)
		/// This has no notable perf impact as Awake is only called once in RW.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
		private void Awake() {
			try {
				Log.Initialize(Logger);
				Log.LogMessage("Loading Dreams of Infinite Glass.");
				Log.LogMessage("Creating error reporter object...");
				_reporter = new ErrorReporter(this);

				// Configuration.Initialize(); // This is now handled by the RemixConfigScreen class.
				Log.LogMessage("Loading configs...");
				_cfgScr = RemixConfigScreen.BIE_Initialize();

				Log.LogMessage("Loading runtime assets...");
				XansAssets.Initialize();

				Log.LogMessage("Initializing all ExtEnums...");
				Oracles.CallToStaticallyReference();
				Sounds.CallToStaticallyReference();
				PlaceableObjects.CallToStaticallyReference();
				MechSaveData.CallToStaticallyReference();

				Log.LogMessage("Performing patches...");
				Log.LogTrace("Shadowed hooks...");
				Harmony harmony = new Harmony(PLUGIN_ID);
				_patcher = new AutoPatcher();
				_patcher.Initialize(harmony);
				GlassOracle_ShadowHooks.MakeShadowHooks(_patcher);
				GlassOracleGraphics_ShadowHooks.MakeShadowHooks(_patcher);

				Log.LogTrace("Standard On/IL hooks...");
				Slugcats.Initialize();
				MechPlayer.Initialize();
				// MechPlayerMechanics.Initialize();
				CustomObjectData.Initialize();
				GlassOraclePatches.Initialize();
				FutileMods.Initialize();

				On.RainWorld.OnModsInit += OnModsInitializing;

				Log.LogMessage("Initialization complete. Have a nice day.");
				Log.LogMessage("TAKE THE NICE DAY? (Y/N) > Y");
				Log.LogMessage("You took the NICE DAY!");

			} catch (Exception exc) {
				Log.LogFatal("WAKE THE FUCK UP SAMURAI. I SHIT THE BED.");
				Log.LogFatal(exc.ToString());
				_reporter.DeferredReportModInitError(exc, $"Loading {PLUGIN_NAME}");
			}
		}

		private void OnModsInitializing(On.RainWorld.orig_OnModsInit originalMethod, RainWorld @this) {
			originalMethod(@this);
			try {
				MachineConnector.SetRegisteredOI(PLUGIN_ID, _cfgScr);
			} catch (Exception exc) {
				Log.LogFatal(exc);
				_reporter.DeferredReportModInitError(exc, $"Registering the Remix config menu to {PLUGIN_NAME}");
				throw;
			}
		}
	}
}