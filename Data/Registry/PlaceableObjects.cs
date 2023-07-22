using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Registry {
	public static class PlaceableObjects {

		/// <summary>
		/// Used to reference the objects by invoking the static constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] // Force the compiler to keep this here and keep calling it anyway.
		internal static void CallToStaticallyReference() { }

		/// <summary>
		/// A type enum for the <see cref="StableZapCoil"/> class.
		/// </summary>
		public static readonly PlacedObject.Type STABLE_ZAP_COIL = new PlacedObject.Type("StableZapCoil", true);

		/// <summary>
		/// A type enum for the <see cref="SuperStructureVacuumTubes"/> class.
		/// </summary>
		public static readonly PlacedObject.Type SUPERSTRUCTURE_VACUUM_TUBES = new PlacedObject.Type("SuperStructureVacuumTubes", true);

	}
}
