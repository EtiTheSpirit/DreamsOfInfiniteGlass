#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamsOfInfiniteGlass.Data.Registry;
using XansTools.Utilities;
using XansTools.Utilities.RW;

namespace DreamsOfInfiniteGlass.Character.NPC.RedFren {
	public class RedFrenRoamingBehavior : AIModule {

		private float _boredTime;

		public RedFrenRoamingBehavior(ArtificialIntelligence ai) : base(ai) {
			_boredTime = 0;
		}

		public override void Update() {
			_boredTime += Mathematical.RW_DELTA_TIME;
		}

		public override void NewRoom(Room room) {
			if (room.abstractRoom == null) {
				AI.creature.Destroy();
				return;
			}
			if (room.abstractRoom.name != "AI" || room.abstractRoom.world.region.name != DreamsOfInfiniteGlassPlugin.REGION_PREFIX) {
				AI.creature.Destroy();
				return;
			}
		}

		public override float Utility() {
			return _boredTime;
		}
	}
}
