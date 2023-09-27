#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamsOfInfiniteGlass.Character.NPC.Iterator;

namespace DreamsOfInfiniteGlass.Character.NPC.RedFren {
	public class RedFrenAI : ArtificialIntelligence {

		private RedFrenRoamingBehavior _roaming;

		private Oracle _glass;

		public RedFrenAI(AbstractCreature creature, World world, Oracle glass) : base(creature, world) {
			AddModule(_roaming = new RedFrenRoamingBehavior(this));
			_glass = glass;
		}

		public override void Update() {
			
		}

		public override bool WantToStayInDenUntilEndOfCycle() {
			return false;
		}

		public override float CurrentPlayerAggression(AbstractCreature player) {
			return 0;
		}
	}
}
