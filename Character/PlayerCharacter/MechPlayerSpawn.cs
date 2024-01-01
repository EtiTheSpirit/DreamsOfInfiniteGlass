#nullable enable
using DreamsOfInfiniteGlass.Data.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public static class MechPlayerSpawn {

		internal static void Initialize() {
			On.SaveState.GetStoryDenPosition += OnGettingStoryDenPosition;
		}

		private static string OnGettingStoryDenPosition(On.SaveState.orig_GetStoryDenPosition originalMethod, SlugcatStats.Name slugcat, out bool isVanilla) {
			string original = originalMethod(slugcat, out isVanilla);

			if (slugcat == Slugcats.MechID) {
				original = DreamsOfInfiniteGlassPlugin.AI_CHAMBER;
				isVanilla = false;
			}

			return original;
		}



	}
}
