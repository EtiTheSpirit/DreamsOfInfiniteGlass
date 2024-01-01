using DreamsOfInfiniteGlass.Data.Creature;
using Noise;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.Character.NPC.Machine {
	public class MaintainenceSpider : Creature, PhysicalObject.IHaveAppendages {
		public MaintainenceSpider(AbstractCreature abstractCreature, World world) : base(abstractCreature, world) {
		}

		public override void Abstractize() {
			base.Abstractize();
		}

		public override bool AllowableControlledAIOverride(MovementConnection.MovementType movementType) {
			return base.AllowableControlledAIOverride(movementType);
		}


		public override bool CanBeGrabbed(Creature grabber) {
			return false;
		}

		public override bool Grab(PhysicalObject obj, int graspUsed, int chunkGrabbed, Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying) {
			return base.Grab(obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
		}

		public override void GrabbedObjectSnatched(PhysicalObject grabbedObject, Creature thief) {
			base.GrabbedObjectSnatched(grabbedObject, thief);
		}

		public override void HeardNoise(InGameNoise noise) {
			base.HeardNoise(noise);
		}

		public override void Blind(int blnd) {
			base.Blind(blnd);
		}

		public override void Stun(int st) {
			base.Stun(st);
		}

		public override void Deafen(int df) {
			base.Deafen(df);
		}

		public override void Die() {
			base.Die();
		}

		public override void LoseAllGrasps() {
			base.LoseAllGrasps();
		}

		public override void NewRoom(Room newRoom) {
			base.NewRoom(newRoom);
		}

		public override void NewTile() {
			base.NewTile();
		}

		public override void PlaceInRoom(Room placeRoom) {
			base.PlaceInRoom(placeRoom);
		}

		public override void PushOutOf(Vector2 pos, float rad, int exceptedChunk) {
			base.PushOutOf(pos, rad, exceptedChunk);
		}

		public override void RecreateSticksFromAbstract() {
			base.RecreateSticksFromAbstract();
		}

		public override void ReleaseGrasp(int grasp) {
			base.ReleaseGrasp(grasp);
		}

		public override Color ShortCutColor() {
			return base.ShortCutColor();
		}

		public override void SpitOutOfShortCut(IntVector2 pos, Room newRoom, bool spitOutAllSticks) {
			base.SpitOutOfShortCut(pos, newRoom, spitOutAllSticks);
		}

		public override void Update(bool eu) {
			base.Update(eu);
		}

		public override bool SpearStick(Weapon source, float dmg, BodyChunk chunk, Appendage.Pos appPos, Vector2 direction) {
			return base.SpearStick(source, dmg, chunk, appPos, direction);
		}

		public override void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact) {
			base.TerrainImpact(chunk, direction, speed, firstContact);
		}

		public override void Violence(BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Appendage.Pos hitAppendage, DamageType type, float damage, float stunBonus) {
			base.Violence(source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
		}

		public override void InitiateGraphicsModule() {
			base.InitiateGraphicsModule();
		}

		public Vector2 AppendagePosition(int appendage, int segment) {
			throw new NotImplementedException();
		}

		public void ApplyForceOnAppendage(Appendage.Pos pos, Vector2 momentum) {
			throw new NotImplementedException();
		}

		#region Body Part Types

		public abstract class Leg : IProvideUpdatesAndGraphics {

			private readonly PhysicalObject _owner;

			private readonly BodyChunk _part;

			public Vector2 TargetPosition { get; set; }
			
			public Leg(PhysicalObject owner, BodyChunk onChunk) {
				_owner = owner;
				_part = onChunk;
			}

			public void UpdateObject(bool eu) {
				throw new NotImplementedException();
			}

			public void UpdateGraphics() {
				throw new NotImplementedException();
			}

			public void ResetObject() {
				throw new NotImplementedException();
			}

			public void ResetGraphics() {
				throw new NotImplementedException();
			}

			public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
				throw new NotImplementedException();
			}

			public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
				throw new NotImplementedException();
			}

			public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
				throw new NotImplementedException();
			}

			public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContainer) {
				throw new NotImplementedException();
			}
		}

		

		#endregion
	}
}
