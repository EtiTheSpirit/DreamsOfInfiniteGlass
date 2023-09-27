using BepInEx.Bootstrap;
using DreamsOfInfiniteGlass.Data.Registry;
using System;
using System.Collections.Generic;
#nullable enable
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Character.NPC.Purposed {

	/// <summary>
	/// This class handles custom overseer behavior, like unique markers/holograms.
	/// </summary>
	public sealed class GlassOverseer : Extensible.Overseer {

		[Obsolete("This code isn't complete yet, finish it first.")]
		internal static void Initialize() {
			
		}


#pragma warning disable IDE0051, IDE0060
		GlassOverseer(Overseer original) : base(original) { }
#pragma warning restore IDE0051, IDE0060
	}
}
