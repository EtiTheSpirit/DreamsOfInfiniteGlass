#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Data.Registry {
	public class Oracles {

		/// <summary>
		/// Used to reference the objects by invoking the static constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] // Force the compiler to keep this here and keep calling it anyway.
		internal static void CallToStaticallyReference() { }

		public static Oracle.OracleID GlassID { get; } = new Oracle.OracleID("DreamsOfInfiniteGlass", true);


	}
}
