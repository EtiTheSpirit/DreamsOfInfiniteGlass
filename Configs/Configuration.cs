#nullable disable
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamsOfInfiniteGlass.Data.Registry;
using static OptionInterface;

namespace DreamsOfInfiniteGlass.Configs {
	public static class Configuration {

		/// <summary>
		/// If true, highly detailed logs will be made.
		/// </summary>
		public static bool TraceLogging => _traceLogging.Value;

		/// <summary>
		/// If true, smooth camera movement is enabled.
		/// </summary>
		public static bool SmoothCamera => _smoothCamera.Value;

		/// <summary>
		/// If true, VRAM usage will be limited by unloading textures.
		/// </summary>
		public static bool LowVRAMMode => _lowVRAM.Value;

		/// <summary>
		/// If true, the rain will no longer fling every physical object in the room around.
		/// This can somewhat break immersion but also resolves a lot of lag.
		/// </summary>
		public static bool DisableRoomRainThrowing => _disableRoomRainThrowing.Value;

		#region Backing Fields

		#region Mod Meta
		private static Configurable<bool> _traceLogging;
		#endregion

		#region Graphics and Systems
		private static Configurable<bool> _smoothCamera;

		private static Configurable<bool> _lowVRAM;
		#endregion

		#region Interop

		#endregion

		#region Player Mechanics

		private static Configurable<bool> _disableRoomRainThrowing;

		#endregion

		#endregion

		#region Config Helpers

		internal static bool Initialized { get; private set; }

		private static ConfigHolder _config;

		private static string _currentSection = "No Section";

		private static List<string> _orderedCategories = new List<string>();
		private static IReadOnlyList<string> _orderedCategoriesCache = null;
		private static readonly Dictionary<string, List<ConfigurableBase>> _allConfigs = new Dictionary<string, List<ConfigurableBase>>();
		private static IReadOnlyDictionary<string, IReadOnlyList<ConfigurableBase>> _allConfigsCache = null;
		private static readonly  Dictionary<string, string> _categoryDescriptions = new Dictionary<string, string>();

		private static void CreateConfig<T>(ref Configurable<T> field, T defaultValue, string name, string description, bool requiresRestart = false) {
			//field = _config.Bind(new ConfigDefinition(_currentSection, name), defaultValue, new ConfigDescription(description));
			string sanitizedName = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
			field = _config.Bind(sanitizedName, defaultValue, new ConfigurableInfo(description, autoTab: _currentSection, tags: new object[] { name, requiresRestart }));
			if (_traceLogging != null) {
				Log.LogTrace($"Registered config entry: {name}");
			}
			_allConfigsCache = null;
			_orderedCategoriesCache = null;
			if (!_allConfigs.TryGetValue(_currentSection, out List<ConfigurableBase> entries)) {
				_orderedCategories.Add(_currentSection);
				entries = new List<ConfigurableBase>();
				_allConfigs[_currentSection] = entries;
			}

			entries.Add(field);
		}

		/// <summary>
		/// Returns a lookup from section => configs in that section. This can be used for Remix.
		/// </summary>
		/// <returns></returns>
		internal static void GetAllConfigs(out IReadOnlyDictionary<string, IReadOnlyList<ConfigurableBase>> lookup, out IReadOnlyList<string> categories) {
			if (_allConfigsCache == null || _orderedCategoriesCache == null) {
				Dictionary<string, IReadOnlyList<ConfigurableBase>> allCfgsRO = new Dictionary<string, IReadOnlyList<ConfigurableBase>>();
				foreach (KeyValuePair<string, List<ConfigurableBase>> entry in _allConfigs) {
					allCfgsRO[entry.Key] = entry.Value.AsReadOnly();
				}
				_allConfigsCache = allCfgsRO;
				_orderedCategoriesCache = _orderedCategories.AsReadOnly();
			}
			lookup = _allConfigsCache;
			categories = _orderedCategoriesCache;
		}

		private static void SetCurrentSectionDescription(string description) => _categoryDescriptions[_currentSection] = description;

		public static string GetCategoryDescription(string cat) {
			if (_categoryDescriptions.TryGetValue(cat, out string categoryDesc)) return categoryDesc;
			return "It seems Xan forgot to put a description on this category.";
		}

		#endregion

		/// <summary>
		/// This should not be called by you. It is called by the remix config screen class of this mod.
		/// </summary>
		/// <param name="cfg"></param>
		/// <exception cref="InvalidOperationException"></exception>
		internal static void Initialize(ConfigHolder cfg) {
			if (Initialized) throw new InvalidOperationException("Configurations have already been initialized!");
			Log.LogMessage("Initializing configuration file.");
			_config = cfg;

			_currentSection = "Mod Meta";
			SetCurrentSectionDescription("Settings that relate to the mod's internal behavior. These settings do not affect gameplay.");

			CreateConfig(ref _traceLogging, false, "Trace Logging", description:
$@"If enabled, logs will be highly detailed. This can negatively impact performance!
You should activate this if you are trying to find bugs in the mod.",
			false);
			Log.LogTrace("TRACE LOGGING IS ENABLED. The logs will be cluttered with information only useful when debugging, and trace entries will incur a performance cost. You have been warned!");

			_currentSection = "Core Systems";
			SetCurrentSectionDescription("Settings that relate to critical internal systems.");

			CreateConfig(ref _smoothCamera, true, "Smooth Camera", description:
$@"/// INCOMPATIBILITY WARNING: Disable SBCameraScroll ///
If enabled, the camera will scroll smoothly in rooms rather than having discrete, 
preset views. Levels in this mod have been explicitly rendered to support this using
a custom renderer.",
			false);

			CreateConfig(ref _lowVRAM, false, "Low VRAM Mode", description:
$@"Enable this option to reduce Video RAM usage. This is only really useful if you
have a comically old (>10 years) GPU, or you are using integrated graphics and have
low RAM. // NOTE: In exchange, this may cause lag when entering rooms in this
mod's region, as it will be streaming textures from disk.",
			false);

			// _currentSection = "Interop";
			//SetCurrentSectionDescription("Settings that relate to interactions between this mod and other mods.");

			_currentSection = "Mechanical Overrides";
			SetCurrentSectionDescription("Changes to how the game works that fundamentally changes it, at the benefit of better mod presentation.");
			CreateConfig(ref _disableRoomRainThrowing, false, "Disable Global Rain Impact Force", description:
$@"While it is fundamental to the game, the rain throwing objects around the room can cause
a noticable amount of lag that affects the experience negatively. Enabling this option will,
for any case where one or more players are SOLSTICE, disable this force. If this is disabled,
the game will still do it but will ignore SOLSTICE.",
			false);

			Initialized = true;
		}
	}
}
