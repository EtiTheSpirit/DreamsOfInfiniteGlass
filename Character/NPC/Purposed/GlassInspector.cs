using MonoMod.RuntimeDetour;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static DreamsOfInfiniteGlass.Character.NPC.Purposed.ColoredInformation;

namespace DreamsOfInfiniteGlass.Character.NPC.Purposed {
	public sealed class GlassInspector : Extensible.MoreSlugcats.Inspector {

		internal static void Initialize() {
			On.MoreSlugcats.Inspector.ctor += OnInspectorConstructing;
		}

		private static void OnInspectorConstructing(On.MoreSlugcats.Inspector.orig_ctor originalCtor, Inspector @this, AbstractCreature abstractCreature, World world) {
			originalCtor(@this, abstractCreature, world);
			Binder<GlassInspector>.Bind(@this);
		}

		GlassInspector(Inspector original) : base(original) { }

		public override Color OwneriteratorColor {
			get {
				if (ownerIterator == 16161616) {
					return GLASS_OVERSEER_COLOR;
				} else {
					return base.OwneriteratorColor;
				}
			}
		}

		public override void InitiateGraphicsModule() {
			if (ownerIterator == -1) {
				if (room.game.IsStorySession && room.world.region != null) {
					if (room.world.region.name == DreamsOfInfiniteGlassPlugin.REGION_PREFIX) {
						ownerIterator = GLASS_OVERSEER_IDENTITY;
					}
				}
			}
			base.InitiateGraphicsModule();
		}


	}
}
