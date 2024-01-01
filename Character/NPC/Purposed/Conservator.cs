#nullable enable
using DreamsOfInfiniteGlass.Character.NPC.Iterator.Interaction;
using DreamsOfInfiniteGlass.Character.PlayerCharacter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansTools.Utilities.General;
using static Conversation;
using static DreamsOfInfiniteGlass.Character.NPC.Iterator.Interaction.GlassConversation;

namespace DreamsOfInfiniteGlass.Character.NPC.Purposed {
	public class Conservator : Creature, IOwnAConversation, IParameterizedEventReceiver {

		private uint _currentInteractionIndex = 0;

		public RainWorld rainWorld { get; }

		public Conservator(AbstractCreature abstractCreature, World world) : base(abstractCreature, world) {
			rainWorld = world.game.rainWorld;
		}

		private uint JumpDelegate_ConservatorInteraction(GlassConversation src, JumpEvent jmp) {
			Player? player = room.PlayersInRoom.FirstOrDefault();
			if (player == null) {
				src.Terminate();
				return 0;
			}
			Extensible.Player.Binder<MechPlayer>.TryGetBinding(player, out MechPlayer mech);
			return 0;
		}

		public void EventFired(ParameterizedEvent evt) {
			throw new NotImplementedException();
		}

		public string ReplaceParts(string s) => s;

		public void SpecialEvent(string eventName) { }

	}
}
