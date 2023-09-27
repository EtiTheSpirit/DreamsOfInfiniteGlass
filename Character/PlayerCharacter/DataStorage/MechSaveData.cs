#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter.DataStorage { 
	public static class MechSaveData {

		public static readonly DeathPersistentSaveData.Tutorial BATTERY_TUTORIAL = new DeathPersistentSaveData.Tutorial("mech_battery", true);

		public static readonly DeathPersistentSaveData.Tutorial RAREFACTION_CELL_TUTORIAL = new DeathPersistentSaveData.Tutorial("mech_rarefaction_recharge", true);

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		internal static void CallToStaticallyReference() {} 

		public static DeathPersistentSaveData? GetPermSaveData(Player player) {
			if (player.room.game.session is StoryGameSession story) {
				return story.saveState.deathPersistentSaveData;
			}
			return null;
		}

		private static bool GetTutorialValue(this DeathPersistentSaveData data, DeathPersistentSaveData.Tutorial key) {
			return data != null && data.tutorialMessages.Contains(key);
		}

		private static bool GetTutorialValueFor(Player player, DeathPersistentSaveData.Tutorial tutorial) {
			DeathPersistentSaveData? permSaveData = GetPermSaveData(player);
			return permSaveData != null && permSaveData.GetTutorialValue(tutorial);
		}

		private static void SetTutorialValueFor(Player player, DeathPersistentSaveData.Tutorial tutorial, bool value) {
			DeathPersistentSaveData? permSaveData = GetPermSaveData(player);
			permSaveData?.SetTutorialValue(tutorial, value);
		}

		public static bool HasShownBatteryTutorial(Player player) {
			return GetTutorialValueFor(player, BATTERY_TUTORIAL);
		}

		public static void SetShownBatteryTutorial(Player player, bool shown) {
			SetTutorialValueFor(player, BATTERY_TUTORIAL, shown);
		}

		public static bool HasShownRarefactionRechargeTutorial(Player player) {
			return GetTutorialValueFor(player, RAREFACTION_CELL_TUTORIAL);
		}

		public static void SetShownRarefactionRechargeTutorial(Player player, bool shown) {
			SetTutorialValueFor(player, RAREFACTION_CELL_TUTORIAL, shown);
		}
	}
}
