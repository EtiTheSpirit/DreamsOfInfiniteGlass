using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Players {
	/// <summary>
	/// A lookup of player stats for use in <see cref="PlayerStorage"/>.
	/// </summary>
	public static class PlayerAdditionalStats {

		public static readonly PlayerStorageKey<float> MECH_BATTERY = new PlayerStorageKey<float>("battery");

	}
}
