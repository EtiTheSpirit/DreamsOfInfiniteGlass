using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Utilities {

	public static class DangerTypeHelper {

		/// <summary>
		/// Attempts to return a replacement <see cref="RoomRain.DangerType"/> that has flooding.
		/// This only modifies <see cref="RoomRain.DangerType.Rain"/> (by returning <see cref="RoomRain.DangerType.FloodAndRain"/>) and
		/// returns <paramref name="type"/> if no changes were made.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static RoomRain.DangerType TryAddFlooding(RoomRain.DangerType type) {
			if (type == RoomRain.DangerType.Rain) {
				return RoomRain.DangerType.FloodAndRain;
			}
			return type;
		}

		/// <summary>
		/// Returns true if the provided danger type creates rain.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool HasRain(RoomRain.DangerType type) {
			return type == RoomRain.DangerType.Rain || type == RoomRain.DangerType.FloodAndRain;
		}
	
	}

}
