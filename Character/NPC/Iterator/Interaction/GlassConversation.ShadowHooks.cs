using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities;

namespace XansCharacter.Character.NPC.Iterator.Interaction {
	public abstract partial class GlassConversation {

		internal static void Initialize(AutoPatcher patcher) {
			// patcher.TurnShadowedMethodIntoOverride<Conversation, GlassConversation>(nameof(Interrupt));
			// patcher.TurnShadowedMethodIntoOverride<Conversation, GlassConversation>(nameof(ForceAddMessage));
		}

	}
}
