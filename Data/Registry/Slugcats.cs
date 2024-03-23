#nullable enable
using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using JollyCoop;
using Menu;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Exceptions;
using XansTools.Utilities.Cecil;
using XansTools.Utilities.RW;

namespace DreamsOfInfiniteGlass.Data.Registry {
	public static class Slugcats {

		[Obsolete("Xan: This constructor allows referencing the fields in IL.", true)]
		static Slugcats() {
			_mechIDField = new SlugcatStats.Name("Mech", true);
			_mechSceneStartField = new MenuScene.SceneID("MechSceneStart", true);
			_mechSceneEndField = new MenuScene.SceneID("MechSceneEnd", true);
			MechID = _mechIDField;
			MechSceneIDNewGame = _mechSceneStartField;
			MechSceneIDEndGame = _mechSceneEndField;
		}

		#region IL Hook Fields

		/// <summary>
		/// This exists for convenience in the IL hook. Use <see cref="MechID"/> instead.
		/// </summary>
		[Obsolete("Xan: Use MechID instead, this is for convenience in the IL hook.", true)]
		private static readonly SlugcatStats.Name _mechIDField;

		/// <summary>
		/// This exists for convenience in the IL hook. Use <see cref="MechSceneIDNewGame"/> instead.
		/// </summary>
		[Obsolete("Xan: Use MechSceneIDNewGame instead, this is for convenience in the IL hook.", true)]
		private static readonly MenuScene.SceneID _mechSceneStartField;

		/// <summary>
		/// This exists for convenience in the IL hook. Use <see cref="MechSceneIDEndGame"/> instead.
		/// </summary>
		[Obsolete("Xan: Use MechSceneIDEndGame instead, this is for convenience in the IL hook.", true)]
		private static readonly MenuScene.SceneID _mechSceneEndField;

		#endregion

		/// <summary>
		/// The cool cat. It's not really a cat, though. Nor is it a slug.
		/// </summary>
		public static SlugcatStats.Name MechID { get; }

		/// <summary>
		/// The ID of the mech scene at the start of the game.
		/// </summary>
		public static MenuScene.SceneID MechSceneIDNewGame { get; }

		/// <summary>
		/// The ID of the mech scene at the end of the game.
		/// </summary>
		public static MenuScene.SceneID MechSceneIDEndGame { get; }

		/// <summary>
		/// The name of the mechanical slugcat.
		/// </summary>
		public static string MechName { get; } = MechPlayer.CHARACTER_NAME;

		/// <summary>
		/// A short description of the mechanical slugcat.
		/// </summary>
		public static string MechDescription { get; } =
@"A uniquely advanced mechanical being that experiences
the world much differently than any ordinary creature.";

		internal static void Initialize() {
			Log.LogMessage("Hooking slugcat related data containers...");
			On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.ctor += OnConstructingNewGamePage;
			On.Menu.SlugcatSelectMenu.SetSlugcatColorOrder += OnSettingSlugcatColorOrder;
			On.PlayerGraphics.DefaultSlugcatColor += OnGettingDefaultSlugcatColor;
			On.PlayerGraphics.ColoredBodyPartList += OnGettingColoredBodyPartList;
			On.PlayerGraphics.DefaultBodyPartColorHex += OnGettingDefaultBodyPartColorList;
			On.Menu.MenuScene.BuildScene += OnBuildingScene;
			On.SlugcatStats.getSlugcatName += OverrideDisplayedName;
			On.SaveState.GetStoryDenPosition += OnGettingStoryPosition;

			Log.LogMessage("Performing runtime code changes to data containers...");
			IL.Menu.SlugcatSelectMenu.SlugcatPage.AddImage += InjectAddImageToSlugcat;
			IL.Menu.SlugcatSelectMenu.RefreshJollySummary += InjectRefreshJollySummary;
			On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += OnJollyMenuPlayerSelectorUpdate;
		}

		private static string OnGettingStoryPosition(On.SaveState.orig_GetStoryDenPosition originalMethod, SlugcatStats.Name slugcat, out bool isVanilla) {
			string originalResult = originalMethod(slugcat, out isVanilla);
			if (slugcat == MechID) {
				isVanilla = false;
				return "HI_B02";
			}
			return originalResult;
		}

		/// <summary>
		/// Returns true if any current player in the game is SOLSTICE.
		/// </summary>
		/// <returns></returns>
		public static bool IsAnyoneMechPlayer(RainWorldGame? game = null) {
			/*
			MainLoopProcess mainLoop = game ?? Custom.rainWorld.processManager.currentMainLoop;
			if (mainLoop is RainWorldGame game1) {
				game = game1;
				return game.Players.Where(abs => abs != null && abs.creatureTemplate == Player).Select(abs => (Player)abs.realizedCreature).Any(plr => MechPlayer.From(plr) is not null);
			}
			throw new InvalidOperationException("Rain World's current process is not the in-game process; getting a player is not possible at this time.");
			*/
			MainLoopProcess mainLoop = game ?? Custom.rainWorld.processManager.currentMainLoop;
			if (mainLoop is RainWorldGame game1) {
				game = game1;
				if (game.IsStorySession && ModManager.CoopAvailable) {
					foreach (AbstractCreature abstractCreature in game.Players) {
						if (abstractCreature.state is PlayerState state) {
							if (game.rainWorld.options.jollyPlayerOptionsArray[state.playerNumber].playerClass == MechID) return true;
						}
					}
				} else {
					foreach (AbstractCreature abstractCreature in game.Players) {
						if (abstractCreature.state is PlayerState state) {
							if (state.slugcatCharacter == MechID) return true;
						}
					}
				}
			}
			return false;
		}

		#region Display Name Changes

		private static void OnJollyMenuPlayerSelectorUpdate(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update originalMethod, JollyCoop.JollyMenu.JollyPlayerSelector @this) {
			originalMethod(@this);

			SlugcatStats.Name name = JollyCustom.SlugClassMenu(@this.index, @this.dialog.currentSlugcatPageName);
			if (name == MechID) {
				@this.classButton.menuLabel.text = MechName;
			}
		}


		private static string OverrideDisplayedName(On.SlugcatStats.orig_getSlugcatName originalMethod, SlugcatStats.Name @this) {
			if (@this == MechID) {
				return MechName;
			}
			return originalMethod(@this);
		}

		private static void InjectRefreshJollySummary(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			bool success = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchLdstr("The "),
				instruction => instruction.MatchLdloc(3),
				instruction => instruction.MatchCall("getSlugcatName"),
				instruction => instruction.MatchCall("Concat"),
				instruction => instruction.MatchCall("Translate")
			);
			if (!success) throw new InvalidOperationException("Failed to locate the instruction prefixing the name of the character with \"The\" in Jolly Co-Op!");
			cursor.Emit(OpCodes.Ldloc_3);
			cursor.EmitDelegate<ReplaceSlugcatNameDelegate>(ReplaceSlugcatName);
		}

		private delegate string ReplaceSlugcatNameDelegate(string result, SlugcatStats.Name name);
		private static string ReplaceSlugcatName(string result, SlugcatStats.Name name) {
			if (name == MechID) {
				return MechName;
			}
			return result;
		}

		#endregion

		#region Scenes and Menus
		private static void InjectAddImageToSlugcat(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			FieldInfo slugcatID = typeof(SlugcatSelectMenu.SlugcatPage).GetField(nameof(SlugcatSelectMenu.SlugcatPage.slugcatNumber), BindingFlags.Instance | BindingFlags.Public);
			// FieldInfo survivorSceneID = typeof(MenuScene.SceneID).GetField(nameof(MenuScene.SceneID.Slugcat_White), BindingFlags.Static | BindingFlags.Public);
			bool found = cursor.TryGotoNext(
				//MoveType.Before,
				MoveType.After,
				instruction => instruction.MatchLdsfld(nameof(MenuScene.SceneID.Slugcat_White)),
				instruction => instruction.MatchStloc(0)//,
				//instruction => instruction.MatchLdarg(0),
				//instruction => instruction.MatchLdfld(nameof(SlugcatSelectMenu.SlugcatPage.slugcatNumber))
			);
			if (!found) {
				throw new RuntimePatchFailureException($"{nameof(SlugcatSelectMenu)}.{nameof(SlugcatSelectMenu.SlugcatPage)}.{nameof(SlugcatSelectMenu.SlugcatPage.AddImage)}", "Failed to match IL code for initial scene reference.");
			}

			Log.LogTrace($"IL cursor is at instruction No. {cursor.Index} ({cursor.Next.ToStringFixed()})");

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldloc_0);
			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldfld, slugcatID);
			cursor.Emit(OpCodes.Ldarg_1);
			cursor.EmitDelegate<InjectCustomSceneIDDelegate>(GetCustomSceneID);
			cursor.Emit(OpCodes.Stloc_0);
		}

		private delegate MenuScene.SceneID InjectCustomSceneIDDelegate(SlugcatSelectMenu.SlugcatPage page, MenuScene.SceneID original, SlugcatStats.Name name, bool ascended);
		private static MenuScene.SceneID GetCustomSceneID(SlugcatSelectMenu.SlugcatPage page, MenuScene.SceneID original, SlugcatStats.Name name, bool ascended) {
			if (name == MechID) {
				page.sceneOffset = new Vector2(-10f, 100f);
				page.slugcatDepth = 3f;
				page.markOffset = new Vector2(-15f, -2f);
				page.glowOffset = new Vector2(-30f, -50f);
				if (ascended) {
					return MechSceneIDEndGame;
				}
				return MechSceneIDNewGame;
			}
			return original;
		}

		private static void OnConstructingNewGamePage(On.Menu.SlugcatSelectMenu.SlugcatPageNewGame.orig_ctor originalMethod, SlugcatSelectMenu.SlugcatPageNewGame @this, Menu.Menu menu, MenuObject owner, int pageIndex, SlugcatStats.Name slugcatNumber) {
			originalMethod(@this, menu, owner, pageIndex, slugcatNumber);

			if (slugcatNumber == MechID) {
				@this.difficultyLabel.text = @this.menu.Translate(MechName.ToUpper());
				@this.difficultyLabel.pos = new Vector2(-1000f, @this.imagePos.y - 249f); // 219 if there's 3 lines
				@this.infoLabel.text = @this.menu.Translate(MechDescription);
				@this.infoLabel.pos = new Vector2(-1000f, @this.imagePos.y - 311f); // 294 if there's 3 lines
			}
		}

		private static void OnBuildingScene(On.Menu.MenuScene.orig_BuildScene originalMethod, MenuScene @this) {
			originalMethod(@this);

			if (@this.sceneID == MechSceneIDNewGame) {
				const string FOLDER_NAME = "scenes/characterselect";
				@this.AddIllustration(new MenuIllustration(@this.menu, @this, FOLDER_NAME, "main", new Vector2(0, -50), false, false));
			}
		}
		private static void OnSettingSlugcatColorOrder(On.Menu.SlugcatSelectMenu.orig_SetSlugcatColorOrder originalMethod, SlugcatSelectMenu @this) {
			originalMethod(@this);

			@this.slugcatColorOrder.Add(MechID);
		}

		#endregion

		#region Physical Appearance

		private static List<string> OnGettingColoredBodyPartList(On.PlayerGraphics.orig_ColoredBodyPartList originalMethod, SlugcatStats.Name slugcatID) {
			List<string> list = originalMethod(slugcatID);
			if (slugcatID == MechID) {
				if (list[0] != "Body") {
					Log.LogWarning($"Something else modified [0] in the body part list for slugcats! Expected \"Body\", got \"{list[0]}\" -- I'll search for it, but things might break!");
					int i = list.IndexOf("Body");
					if (i == -1) {
						Log.LogWarning("\"Body\" does not exist! Replacement will fail, and color customization may display invalid body parts.");
					} else {
						list[i] = "Chassis";
						Log.LogMessage($"Found \"Body\" at list[{i}], so that was replaced instead.");
					}
				} else {
					list[0] = "Chassis"; // replace Body
				}
				list.Add("Power Rails");

			}
			return list;
		}

		private static Color OnGettingDefaultSlugcatColor(On.PlayerGraphics.orig_DefaultSlugcatColor originalMethod, SlugcatStats.Name slugcatID) {
			if (slugcatID == MechID) {
				return new Color(0.5f, 0.5f, 0.5f, 1.0f);
			}
			return originalMethod(slugcatID);
		}


		private static List<string> OnGettingDefaultBodyPartColorList(On.PlayerGraphics.orig_DefaultBodyPartColorHex orig, SlugcatStats.Name slugcatID) {
			//throw new NotImplementedException();
			return orig(slugcatID);
		}

		#endregion

	}
}
