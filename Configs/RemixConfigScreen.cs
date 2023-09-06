using BepInEx.Configuration;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities.General;

namespace DreamsOfInfiniteGlass.Configs {
	internal class RemixConfigScreen : OptionInterface {

		const float WINDOW_HEIGHT = 630f - 34f; // 630px on the screen minus a 34px margin from the mod settings panel to the top of the window
		const float WINDOW_WIDTH = 630f - 34f; // I honestly don't know why -34 is needed here.
											   // The facts are present though, for example, the mod config panel *is* a perfect square so width should equal height.
											   // It's a mystery that doesn't *really* need to be solved right now.

		/// <summary>
		/// The initializer for configs.
		/// </summary>
		/// <returns></returns>
		internal static RemixConfigScreen BIE_Initialize() {
			RemixConfigScreen cfg = new RemixConfigScreen();
			Configuration.Initialize(cfg.config);
			// Kind of a yucky hack here
			EarlyLoadConfigs(cfg.config);
			return cfg;
		}

		/// <summary>
		/// Sets some data associated with the config file's loading cycle to allow it to load immediately upon calling this method.
		/// </summary>
		/// <param name="cfg"></param>
		private static void EarlyLoadConfigs(ConfigHolder cfg) {
			Type userData = typeof(Kittehface.Framework20.UserData);
			const string PATH = "PersistentDataPath";

			string oldModID = cfg.owner.mod.id;
			string oldPath = userData.GetStatic<string>(PATH);
			string oldDir = ConfigHolder.configDirPath;

			// Set data
			userData.SetStatic(PATH, Application.persistentDataPath);
			cfg.owner.mod.id = DreamsOfInfiniteGlassPlugin.PLUGIN_ID;
			ConfigHolder.configDirPath = null; // Set to null so it re-evalulates

			cfg.Reload(); // Forcefully load these ASAP

			// Now reset data
			cfg.owner.mod.id = oldModID;
			userData.SetStatic(PATH, oldPath);
			ConfigHolder.configDirPath = oldDir;
		}
		private static Vector2 RelativeOffset(float relX, float relY, float absX = 0, float absY = 0) {
			float x = relX + (absX / WINDOW_WIDTH);
			float y = relY + (absY / WINDOW_HEIGHT);
			return new Vector2(x * WINDOW_WIDTH, (1 - y) * WINDOW_HEIGHT);
		}

		private static (string, bool) GetDataFromCfgTags(object[] tags, string key) {
			string displayName = key;
			bool requiresRestart = false;
			if (tags.Length >= 2) {
				if (tags[1] is bool b) {
					requiresRestart = b;
				}
			}
			if (tags.Length >= 1) {
				if (tags[0] is string str) {
					displayName = str;
				}
			}
			return (displayName, requiresRestart);
		}

		public override void Initialize() {
			base.Initialize();
			Configuration.GetAllConfigs(out IReadOnlyDictionary<string, IReadOnlyList<ConfigurableBase>> configs, out IReadOnlyList<string> categories);
			OpTab[] tabs = new OpTab[categories.Count];
			UIconfig last = null;

			// Default small font is 9px with 6px line spacing.
			FTextParams textParams = new FTextParams();
			if (InGameTranslator.LanguageID.UsesLargeFont(Custom.rainWorld.inGameTranslator.currentLanguage)) {
				textParams.lineHeightOffset = -12f;
			} else {
				textParams.lineHeightOffset = -3f;
			}
			for (int i = 0; i < configs.Count; i++) {
				float height = 58;
				string category = categories[i];
				OpTab tab = new OpTab(this, Translate(category));
				OpLabel catName = new OpLabel(RelativeOffset(0, 0, 0, 15), new Vector2(WINDOW_WIDTH, 30), Translate(category), FLabelAlignment.Center, true, null);
				OpLabel desc = new OpLabel(RelativeOffset(0, 0, 0, 36), new Vector2(WINDOW_WIDTH, 30), Translate(Configuration.GetCategoryDescription(category)), FLabelAlignment.Center, false, null);
				tab.AddItems(catName, desc);
				if (category == "Interop") {
					OpLabel desc2 = new OpLabel(RelativeOffset(0, 0, 0, 96), new Vector2(WINDOW_WIDTH, 30), Translate("DANGER: THESE SETTINGS MAY CAUSE BUGS *IN THE MODS THEY CHANGE*.\nBy enabling any of these settings you understand that you MUST NOT, UNDER ANY CIRCUMSTANCES, report bugs in the mods these affect to their respective creators!\n...And I mean, why would you, right? It's literally my mod going in and changing their code. It'd be a bit like telling me to go break someone's window, and then when it breaks, you tell them that they made a mistake that caused their window to break and it's their fault."), FLabelAlignment.Center, false, null);
					tab.AddItems(desc2);
				}

				OpLabel optionName;
				OpLabel optionDesc;
				OpUpdown upDown;
				foreach (ConfigurableBase cfg in configs[category]) {
					(string name, bool requiresRestart) = GetDataFromCfgTags(cfg.info.Tags, cfg.key);
					name = Translate(name);
					if (requiresRestart) {
						name += $" [{Translate("Requires Restart")}]";
					}
					string description = Translate(cfg.info.description);
					int descLines = description.Count(chr => chr == '\n') + 1;
					float lineHeight = 6f + textParams.lineHeightOffset;
					float descLineHeight = (descLines * 9f) + Mathf.Max(((descLines - 1) * lineHeight), lineHeight * 0.5f);
					float descLineOffset = descLineHeight;// * 0.5f;
					switch (ValueConverter.GetTypeCategory(cfg.settingType)) {
						case ValueConverter.TypeCategory.Boolean:
							OpCheckBox box = new OpCheckBox((Configurable<bool>)cfg, RelativeOffset(0.05f, 0, 0, height)) {
								description = description,
								sign = i
							};
							UIfocusable.MutualVerticalFocusableBind(box, last ?? box);
							optionName = new OpLabel(RelativeOffset(0.05f, 0, 30f, height), new Vector2(240f, 30f), name, FLabelAlignment.Left, false, textParams) {
								bumpBehav = box.bumpBehav,
								description = box.description
							};
							optionDesc = new OpLabel(RelativeOffset(0.025f, 0, 30f, height + descLineOffset), new Vector2(440f, descLineHeight), description, FLabelAlignment.Left, false, textParams) {
								bumpBehav = box.bumpBehav,
								description = box.description
							};
							tab.AddItems(box, optionName, optionDesc);
							height += 30f + descLineHeight;
							break;
						case ValueConverter.TypeCategory.Integrals:
							upDown = new OpUpdown((Configurable<int>)cfg, RelativeOffset(0.05f, 0, 0, height), WINDOW_WIDTH * 0.1f) {
								description = description,
								sign = i
							};
							UIfocusable.MutualVerticalFocusableBind(upDown, last ?? upDown);
							optionName = new OpLabel(RelativeOffset(0.16f, 0, 0, height), new Vector2(170f, 36f), name, FLabelAlignment.Left, false, textParams) {
								bumpBehav = upDown.bumpBehav,
								description = upDown.description
							};
							optionDesc = new OpLabel(RelativeOffset(0.025f, 0, 30f, height + descLineOffset), new Vector2(440f, descLineHeight), description, FLabelAlignment.Left, false, textParams) {
								bumpBehav = upDown.bumpBehav,
								description = upDown.description
							};
							tab.AddItems(upDown, optionName, optionDesc);
							height += 6f + 30f + descLineHeight;
							break;
						case ValueConverter.TypeCategory.Floats:
							upDown = new OpUpdown((Configurable<float>)cfg, RelativeOffset(0.05f, 0, 0, height), WINDOW_WIDTH * 0.1f) {
								description = description,
								sign = i
							};
							UIfocusable.MutualVerticalFocusableBind(upDown, last ?? upDown);
							optionName = new OpLabel(RelativeOffset(0.16f, 0, 0, height), new Vector2(170f, 36f), name, FLabelAlignment.Left, false, textParams) {
								bumpBehav = upDown.bumpBehav,
								description = upDown.description
							};
							optionDesc = new OpLabel(RelativeOffset(0.025f, 0, 30f, height + descLineOffset), new Vector2(440f, descLineHeight), description, FLabelAlignment.Left, false, textParams) {
								bumpBehav = upDown.bumpBehav,
								description = upDown.description
							};
							tab.AddItems(upDown, optionName, optionDesc);
							height += 6f + 30f + descLineHeight;
							break;
						default:
							Log.LogWarning($"Config menu type for {cfg.settingType.FullName} has not been declared yet.");
							break;
					}
				}

				tabs[i] = tab;
			}

			
			Tabs = tabs;
		}

		public override string ValidationString() {
			return base.ValidationString();
		}

	}
}
