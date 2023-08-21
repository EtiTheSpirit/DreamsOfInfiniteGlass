﻿using HUD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Data.Registry;

namespace XansCharacter.Character.NPC.Iterator.Interaction {
	public static class GlassConversations {

		private static readonly Conversation.ID DYNAMIC_CONVERSATION = new Conversation.ID("DynamicallyAssembledConversation", true);

		public static GlassConversation ImBack(GlassOracleBehavior gls) {
			return new GlassAnyConversation(gls, Path.Combine("debug", "imback"));
		}

		private class GlassAnyConversation : GlassConversation {

			private string _identity;

			public GlassAnyConversation(GlassOracleBehavior glass, string filePathID) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) {
				_identity = filePathID;
			}

			public override void AddCustomEvents() {
				LoadGlassConversation(_identity, null);
			}
		}

	}
}
