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
		public static bool TraceLogging => _traceLogging.Value; // Default to true until configs load.

		/// <summary>
		/// If true, advanced shader systems that modify behavior on the fly are enabled.
		/// </summary>
		public static bool AdvancedShaderSystems => _advancedShaders.Value;

		#region Backing Fields

		#region Mod Meta
		private static Configurable<bool> _traceLogging;
		#endregion

		#region Graphics and Systems
		private static Configurable<bool> _advancedShaders;
		#endregion

		#region Interop

		#endregion

		#region Player Mechanics

		#endregion

		#endregion

		#region Config Helpers

		internal static bool Initialized { get; private set; }

		private static ConfigHolder _config;

		private static string _currentSection = "No Section";

		private static List<string> _orderedCategories = new List<string>();
		private static IReadOnlyList<string> _orderedCategoriesCache = null;
		private static Dictionary<string, List<ConfigurableBase>> _allConfigs = new Dictionary<string, List<ConfigurableBase>>();
		private static IReadOnlyDictionary<string, IReadOnlyList<ConfigurableBase>> _allConfigsCache = null;
		private static Dictionary<string, string> _categoryDescriptions = new Dictionary<string, string>();

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

			CreateConfig(ref _advancedShaders, true, "Advanced Shaders", description:
$@"This mod makes heavy use of custom shaders. This option will enable advanced features
that can dramatically improve performance in this mod's regions, but which might also
break the renderer in certain weird edge cases that could not be found during testing.",
			false);

			// _currentSection = "Interop";
			//SetCurrentSectionDescription("Settings that relate to interactions between this mod and other mods.");

			

			Initialized = true;
		}
	}
}
