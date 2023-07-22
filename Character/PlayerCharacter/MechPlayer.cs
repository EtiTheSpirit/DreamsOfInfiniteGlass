using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.Data.Registry;
using XansTools.Exceptions;
using XansTools.Utilities.Attributes;
using XansTools.Utilities.RW;

namespace XansCharacter.Character.PlayerCharacter {
	public class MechPlayer : Player {

		#region Shadowed Override Properties

		[ShadowedOverride]
		public new float Adrenaline => mushroomEffect;

		[ShadowedOverride]
		public new bool KarmaIsReinforced => false;

		[ShadowedOverride]
		public new bool PlaceKarmaFlower => false;

		[ShadowedOverride]
		public new bool isSlugpup => false;

		#endregion

		public MechPlayerState MechState => State as MechPlayerState;

		#region Construction

		internal static void Initialize() {
			On.Player.ctor += BeforeConstructor;
			//On.AbstractCreature.Realize += OnRealizingCreature;
			IL.AbstractCreature.Realize += InjectRealizingCreature;
		}

		private static void InjectRealizingCreature(ILContext il) {
			ILCursor cursor = new ILCursor(il);
			bool foundPlayerCtorCall = cursor.TryGotoNext(instruction => instruction.MatchNewobj<Player>());
			if (!foundPlayerCtorCall) throw new RuntimePatchFailureException("Player..ctor");

			ILLabel originalCtor = cursor.MarkLabel();
			cursor.GotoNext();
			ILLabel setRealized = cursor.MarkLabel();
			cursor.GotoPrev();

			Log.LogTrace($"Original ctor instruction: {originalCtor.Target}");
			Log.LogTrace($"set_realizedCreature call: {setRealized.Target}");

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.EmitDelegate<IsRealizingPlayerCharacterDelegate>(IsRealizingPlayerCharacter);
			cursor.Emit(OpCodes.Brfalse_S, originalCtor);
			cursor.Emit(OpCodes.Newobj, typeof(MechPlayer).GetConstructor(new Type[] { typeof(AbstractCreature), typeof(World) }));
			cursor.Emit(OpCodes.Br_S, setRealized);
		}

		private delegate bool IsRealizingPlayerCharacterDelegate(AbstractCreature @this);
		private static bool IsRealizingPlayerCharacter(AbstractCreature @this) {
			if (@this.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat) {
				if (@this.abstractAI == null || !(@this.abstractAI.RealAI is SlugNPCAI)) {
					return true;
				}
			}
			return false;
		}

		private static void BeforeConstructor(On.Player.orig_ctor originalCtor, Player @this, AbstractCreature abstractCreature, World world) {
			PlayerState original = abstractCreature.state as PlayerState;
			if (original.slugcatCharacter == Slugcats.MechID) {
				abstractCreature.state = new MechPlayerState(abstractCreature, original.playerNumber, original.slugcatCharacter, original.isGhost);
			}

			originalCtor(@this, abstractCreature, world);
		}

		public MechPlayer(AbstractCreature abstractCreature, World world) : base(abstractCreature, world) {
			airFriction = 0.5f;
			bounce = 0f;
			surfaceFriction = 0.8f;
			waterFriction = 0.7f;
			buoyancy = -0.5f;

			Log.LogTrace("If you are seeing this, the player was successfully constructed as MechPlayer.");
		}

		#endregion

		/// <summary>
		/// Show a message to the player at the bottom of the screen.
		/// </summary>
		/// <param name="message"></param>
		public static void ShowMessage(string message, bool darkenScreen, bool hideHUD) {
			WorldTools.CurrentCamera.hud.textPrompt.AddMessage(message, 240, 480, darkenScreen, hideHUD);
		}

		#region Shadowed Override Methods

		[ShadowedOverride]
		public new bool CanMaulCreature(Creature creature) {
			return false;
		}

		[ShadowedOverride]
		public new void setPupStatus(bool isPup) {
			if (isPup) {
				Log.LogWarning("Rejected attempt to turn Mech into slugpup. This is not possible.");
				MechState.isPup = false;
				float massFactor = 0.75f * slugcatStats.bodyWeightFac;
				bodyChunks[0].mass = massFactor / 2f;
				bodyChunks[1].mass = massFactor / 2f;
				bodyChunkConnections[0] = new BodyChunkConnection(bodyChunks[0], bodyChunks[1], 17f, BodyChunkConnection.Type.Normal, 1f, 0.5f);
			}
		}

		[ShadowedOverride]
		public new bool AllowGrabbingBatflys() {
			return false;
		}

		[ShadowedOverride]
		public new bool CanEatMeat(Creature creature) {
			return false;
		}


		[ShadowedOverride]
		public new float DeathByBiteMultiplier() {
			return 0; // Custom health system will be used probably
		}

		#endregion

		#region Creature Stats and Behavior
		public override void Deafen(int df) {
			df >>= 2;
			if (df < 40) df = 0;

			base.Deafen(df);
		}
		#endregion

		#region Systems Implmentation
		public override void InitiateGraphicsModule() {
			base.InitiateGraphicsModule();
		}

		public override void Update(bool eu) {
			base.Update(eu);
			buoyancy = -0.5f;
			airFriction = 0.5f;
		}
		#endregion
	}
}
