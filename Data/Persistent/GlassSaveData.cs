using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities;
using XansTools.Utilities.RW;
using XansTools.Utilities.RW.DataPersistence;

namespace DreamsOfInfiniteGlass.Data.Persistent {
	
	/// <summary>
	/// Save data for the character and campaign.
	/// </summary>
	public class GlassSaveData : ISaveable {

		public string DataKey { get; } = "MechPlayerSaveData";

		public static GlassSaveData Instance { get; } = new GlassSaveData();

		private GlassSaveData() {
			DreamsOfInfiniteGlassPlugin.SaveData.OnGameSaving += OnGameSaving;
			DreamsOfInfiniteGlassPlugin.SaveData.OnGameLoaded += OnGameLoaded;
			DreamsOfInfiniteGlassPlugin.SaveData.OnCycleEnded += OnCycleEnded;
		}


		// TODO: You still need to verify that this works.
		/*
		 * EXPECTATIONS:
		 *	- Spawning in (from main menu) should display load for all three contexts.
		 *	- Should spawning in after pressing continue post-cycle load? I think so.
		 *	- Dying and pressing continue should cause CYCLE ENDED
		 *	- Using the shelter should cause CYCLE ENDED, SAVE (SLUGCAT), SAVE (GLOBAL).
		 * 
		 * Effectively, make sure I can use the events to properly manage data.
		 * You also expect potential "leaks" i.e. if you exit and load into another slugcat, is that data appropriately cleared?
		 */

		private void OnCycleEnded(SaveDataAccessor accessor, bool survivedCycle, bool isMalnourished) {
			Log.LogWarning($"CYCLE ENDED: Survived? {survivedCycle} // Malnourished? {isMalnourished}");
		}

		private void OnGameLoaded(SaveDataAccessor accessor, SaveScope scope) {
			Log.LogWarning($"GAME LOADED, SCOPE: {scope}");
		}

		private void OnGameSaving(SaveDataAccessor accessor, SaveScope scope, bool survived, bool isMalnourished) {
			Log.LogWarning($"GAME SAVED, SCOPE: {scope} // Survived? {survived} // Malnourished? {isMalnourished}");
		}

		/// <summary>
		/// Whether or not the player has exploded before.
		/// </summary>
		public bool HasMechPlayerExploded { get; set; }

		/// <summary>
		/// Whether or not the player has been told that they won't die to the rain.
		/// </summary>
		public bool HasSeenRainImmunityTutorial { get; set; }

		/// <summary>
		/// Whether or not the player has been told that they should stay underwater to avoid being thrown around.
		/// </summary>
		public bool HasSeenRainThrowTip { get; set; }

		/// <summary>
		/// Whether or not the player has been told how water works for them.
		/// </summary>
		public bool HasSeenWaterTutorial { get; set; }

		/// <summary>
		/// Expected to be called by <see cref="MechPlayer.Update"/>, this handles updating relevant data within
		/// this object that is sensitive to time and/or game context.
		/// </summary>
		internal void Update() {
		}

		public void SaveToStream(SaveScope scope, BinaryWriter writer) {
			writer.Write(HasMechPlayerExploded);
			writer.Write(HasSeenRainImmunityTutorial);
		}

		public void ReadFromStream(SaveScope scope, BinaryReader reader) {
			HasMechPlayerExploded = reader.ReadBoolean();
			HasSeenRainImmunityTutorial = reader.ReadBoolean();
		}

	}

}
