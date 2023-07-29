using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Registry {
	public static class Sounds {

		/// <summary>
		/// This is an empty method body and does nothing. It is simply used to ensure that the class's static constructor 
		/// <c>.cctor</c> is called before audio initializes.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] // Force the compiler to keep this here and keep calling it anyway.
		internal static void CallToStaticallyReference() { }

		public static readonly SoundID CONDUIT_BUZZ_LOOP = new SoundID("glass_conduitbuzz_LOOP", true);

		public static readonly SoundID STOCK_ZAP_SOUND = new SoundID("glass_stock_shock", true);

		public static readonly SoundID INDUSTRIAL_ALARM = new SoundID("glass_industrial_alarm", true);

		public static readonly SoundID DEVICE_WARNING_LOOP = new SoundID("glass_devicewarn_LOOP", true);

		public static readonly SoundID GIGA_BOOM = new SoundID("glass_gigaboom", true);

		public static readonly SoundID SINGULARITY_MERGED_CHARGE = new SoundID("glass_singularity", true);

		public static readonly SoundID MECH_BROKEN_LOOP = new SoundID("glass_mech_broken", true);
		

	}
}
