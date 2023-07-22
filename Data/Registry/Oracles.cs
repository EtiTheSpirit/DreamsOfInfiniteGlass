using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace XansCharacter.Data.Registry {
	public class Oracles {

		/// <summary>
		/// Used to reference the objects by invoking the static constructor.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)] // Force the compiler to keep this here and keep calling it anyway.
		internal static void CallToStaticallyReference() { }

		public static readonly Oracle.OracleID GLASS_ORACLE_ID = new Oracle.OracleID("DreamsOfInfiniteGlass", true);

		[Obsolete("Conversation IDs are not used under the modified dialogue system.", true)]
		public static readonly Conversation.ID GLASS_EXPOSITION_CONVERSATION_ID = new Conversation.ID("exposition", true);

		[Obsolete("Conversation IDs are not used under the modified dialogue system.", true)]
		public static readonly Conversation.ID GLASS_CLOSING_THOUGHTS_CONVERSATION_ID = new Conversation.ID("closingthoughts", true);

	}
}
