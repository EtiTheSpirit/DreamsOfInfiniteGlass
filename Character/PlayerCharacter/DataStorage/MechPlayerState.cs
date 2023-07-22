using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.Attributes;

namespace XansCharacter.Character.PlayerCharacter.DataStorage {
	public class MechPlayerState : PlayerState {

		//[ShadowedOverride]
		//public new bool permaDead { get; set; }

		public bool alreadyDidDeathExplosionFX = false;

		// TODO: Why is this class used when a significant amount of stats are on the main class?

		public MechPlayerState(AbstractCreature crit, int playerNumber, SlugcatStats.Name slugcatCharacter, bool isGhost) : base(crit, playerNumber, slugcatCharacter, isGhost) { }


	}
}
