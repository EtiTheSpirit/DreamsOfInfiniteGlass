using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Character.PlayerCharacter.Hooks {
	public static partial class MechPlayerMechanics {

		public static bool AllowGrabbingBatflys(Player @this) {
			return false;
		}

		public static bool CanEatMeat(Player @this, Creature from) {
			return false;
		}
	}
}
