#nullable enable
using HUD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DreamsOfInfiniteGlass.Data.Registry;

namespace DreamsOfInfiniteGlass.Character.NPC.Iterator.Interaction {
	public static class GlassConversations {

		private static readonly Conversation.ID DYNAMIC_CONVERSATION = new Conversation.ID("glass:DynamicallyAssembledConversation", true);

		public static GlassConversation ImBack(GlassOracleBehavior gls) {
			return new GlassAnyConversation(gls, Path.Combine("debug", "imback"));
		}

		public static GlassConversation VeryFunnyConversation(GlassOracleBehavior gls) {
			return new GlassAnyConversation(gls, Path.Combine("debug", "cursedending"));
		}

		public static GlassConversation TripleAffirmative(GlassOracleBehavior gls) {
			return new GlassAnyConversation(gls, Path.Combine("pearl", "tripleaffirmativereal"));
		}

		private class GlassAnyConversation : GlassConversation {

			private readonly string _identity;

			public GlassAnyConversation(GlassOracleBehavior glass, string filePathID) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) {
				_identity = filePathID;
			}

			public override void AddCustomEvents() {
				LoadGlassConversation(_identity, null);
			}
		}

	}
}
