using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Data.Registry;

namespace XansCharacter.Character.NPC.Iterator.Interaction {
	public static class GlassConversations {

		private static readonly Conversation.ID DYNAMIC_CONVERSATION = new Conversation.ID("DynamicallyAssembledConversation", true);

		public class GlassExpositionConversation : GlassConversation {

			public GlassExpositionConversation(GlassOracleBehavior glass) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) { }

			public override void AddCustomEvents() {
				LoadGlassConversation("exposition", null);
			}

		}

		public class GlassClosingThoughtsConversation : GlassConversation {

			public GlassClosingThoughtsConversation(GlassOracleBehavior glass) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) { }

			public override void AddCustomEvents() {
				LoadGlassConversation("closingthoughts", null);
			}

		}

		public class VeryFunnyConversation : GlassConversation {

			public VeryFunnyConversation(GlassOracleBehavior glass) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) { }

			public override void AddCustomEvents() {
				LoadGlassConversation("cursedending", null);
			}

		}

		public class TestConversation : GlassConversation {
			public TestConversation(GlassOracleBehavior glass) : base(glass, DYNAMIC_CONVERSATION, glass.dialogBox, glass.player) { }

			public override void AddCustomEvents() {
				LoadGlassConversation("testconversation", null);
			}
		}

	}
}
