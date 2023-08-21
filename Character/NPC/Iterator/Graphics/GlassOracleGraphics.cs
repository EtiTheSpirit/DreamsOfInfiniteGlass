using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansTools.Utilities.Attributes;
using XansTools.Utilities.RW;
using XansTools.Utilities;
using XansCharacter.Data.Registry;
using RWCustom;
using XansCharacter.LoadedAssets;

namespace XansCharacter.Character.NPC.Iterator.Graphics {
	public sealed class GlassOracleGraphics : Extensible.OracleGraphics {

		internal static void Initialize() {
			On.OracleGraphics.ctor += (originalMethod, @this, ow) => {
				originalMethod(@this, ow);
				if (@this.oracle.ID == Oracles.GlassID) {
					Binder<GlassOracleGraphics>.Bind(@this, ow);
				}
			};
			On.OracleGraphics.Gown.Color += GetGownColor;
			On.OracleGraphics.Halo.DrawSprites += OnDrawHaloSprites;
			On.OracleGraphics.Halo.InitiateSprites += OnBuildHaloSprites;
			On.OracleGraphics.Halo.Update += OnHaloUpdating;
		}

		GlassOracleGraphics(OracleGraphics original, PhysicalObject ow) : base(original) {
			Log.LogDebug("Extensible.OracleGraphics for Dreams of Infinite Glass constructed.");
			
			totalSprites = 0;

			gown = new OracleGraphics.Gown(this);
			head = NewRadialBodyPart(5f, 0.995f);
			hands = new GenericBodyPart[2];
			feet = new GenericBodyPart[2];

			knees = new Vector2[2, 2];
			for (int x = 0; x < 2; x++) {
				for (int y = 0; y < 2; y++) {
					knees[x, y] = oracle.firstChunk.pos;
				}
			}
			for (int index = 0; index < 2; index++) {
				hands[index] = NewRadialBodyPart(2f, 0.98f);
				feet[index] = NewRadialBodyPart(2f, 0.98f);
			}

			Oracle.OracleArm.Joint[] armJoints = oracle.arm.joints;
			armJointGraphics = new OracleGraphics.ArmJointGraphics[armJoints.Length];

			for (int i = 0; i < armJoints.Length; i++) {
				AllocateSpritesFromObject(ref totalSprites, ref armJointGraphics[i], new OracleGraphics.ArmJointGraphics(this, armJoints[i], totalSprites), joint => joint.totalSprites);
			}

			AllocateSpritesFromObject(ref firstUmbilicalSprite, ref umbCord, new OracleGraphics.UbilicalCord(this, totalSprites), cord => cord.totalSprites);
			AllocateSprites(ref firstBodyChunkSprite, 2);
			AllocateSprites(ref neckSprite);
			AllocateSprites(ref firstFootSprite, 4);
			AllocateSpritesFromObject(ref totalSprites, ref halo, new OracleGraphics.Halo(this, totalSprites), halo => halo.totalSprites);
			AllocateSprites(ref robeSprite);
			AllocateSprites(ref firstHandSprite, 4);
			AllocateSprites(ref firstHeadSprite, 11); // Vanilla used 10, offset due to HoopSprite
			AllocateSprites(ref fadeSprite);
			AllocateSpritesFromObject(ref firstArmBaseSprite, ref armBase, new OracleGraphics.ArmBase(this, totalSprites), arm => arm.totalSprites);

			Log.LogTrace(totalSprites);
			Log.LogTrace(firstUmbilicalSprite);
			Log.LogTrace(firstBodyChunkSprite);
			Log.LogTrace(neckSprite);
			Log.LogTrace(firstFootSprite);
			Log.LogTrace(robeSprite);
			Log.LogTrace(firstHandSprite);
			Log.LogTrace(firstHeadSprite);
			Log.LogTrace(fadeSprite);
			Log.LogTrace(firstArmBaseSprite);
		}

		#region Constant Data

		/// <summary>
		/// The base color of Glass's body.
		/// </summary>
		public static Color GLASS_BODY_COLOR => new Color(0.774f, 1.000f, 0.349f);

		/// <summary>
		/// The base (darker) color of Glass's robes. This is the color used around the chest area.
		/// </summary>
		public static Color GLASS_ROBE_COLOR_BASE => new Color(1.000f, 0.470f, 0.100f);

		/// <summary>
		/// The highlight (brighter) color of Glass's robes. This is the color used at the end of the sleeves and at the bottom of the gown.
		/// </summary>
		public static Color GLASS_ROBE_COLOR_HIGHLIGHT => new Color(1.000f, 0.650f, 0.420f);

		/// <summary>
		/// The color of Glass's sparks/halo whilst idle.
		/// </summary>
		public static Color GLASS_SPARK_COLOR_IDLE => _sparkColorOverride1;
		private static readonly Color _sparkColorOverride1 = new Color(166f/385f, 224/385f, 213/385f, 0.7f).AlphaAsIntensity();

		/// <summary>
		/// The color of Glass's sparks/halo whilst busy/processing.
		/// </summary>
		public static Color GLASS_SPARK_COLOR_BUSY => _sparkColorOverride2;
		private static readonly Color _sparkColorOverride2 = new Color(0.7f, 0.2f, 0.1f, 1.0f).AlphaAsIntensity();

		/// <summary>
		/// The color of the room lights when Glass is busy (variant 1, for the upper lights)
		/// </summary>
		private static Color GLASS_AI_CHAMBER_CLR_BUSY_A => new Color(1f, 0.425f, 0.100f);

		/// <summary>
		/// The color of the room lights when Glass is busy (variant 2, for the lower lights)
		/// </summary>
		private static Color GLASS_AI_CHAMBER_CLR_BUSY_B => new Color(1f, 0.325f, 0.050f);

		/// <summary>
		/// The color of the room lights when Glass is idle (variant 1, for the upper lights)
		/// </summary>
		private static Color GLASS_AI_CHAMBER_CLR_IDLE_A => new Color(0.832f, 0.895f, 0.947f);

		/// <summary>
		/// The color of the room lights when Glass is idle (variant 2, for the lower lights)
		/// </summary>
		private static Color GLASS_AI_CHAMBER_CLR_IDLE_B => new Color(0.849f, 0.853f, 1.000f);


		#endregion

		/// <summary>
		/// The lights that illuminate the room.
		/// </summary>
		private readonly LightSource[] _roomLights = new LightSource[4];

		/// <summary>
		/// The last known processing state, for changes to the halo color.
		/// </summary>
		private bool? _lastKnownProcessingState = null;

		/// <summary>
		/// The current focus level, where 0 is idle and 1 is busy. This affects the color of the chamber.
		/// </summary>
		private float _focusLevel = 0.0f;

		#region Helper Methods for Sprite Indexing and Creation

		public override int FootSprite(int side, int part) {
			return firstFootSprite + side * 2 + part;
		}

		public override int HandSprite(int side, int part) {
			return firstHandSprite + side * 2 + part;
		}
		public override int PhoneSprite(int side, int part) {
			if (side == 0) {
				return firstHeadSprite + part;
			}
			return firstHeadSprite + 8 + part;
		}

		public override int EyeSprite(int eyeIndex) {
			return firstHeadSprite + 6 + eyeIndex;
		}

		public override int HeadSprite => firstHeadSprite + 4; // Vanilla did 3, offset due to HoopSprite

		public override int ChinSprite => firstHeadSprite + 5; // Vanilla did 4, offset due to HoopSprite

		public int HoopSprite => firstHeadSprite + 3; // This takes 3 so that it draws at the earliest stage, behind everything else.

		public int ForeheadBackgroundSprite => firstHeadSprite + 12; // Vanilla did 11, offset due to HoopSprite

		public int ForeheadSymbolSprite => ForeheadBackgroundSprite + 1;

		public override int MoonThirdEyeSprite => ForeheadBackgroundSprite;

		public override int MoonSigilSprite => ForeheadSymbolSprite;

		private GenericBodyPart NewRadialBodyPart(float radius, float friction) => new GenericBodyPart(this, radius, 0.5f, friction, oracle.firstChunk);

		/// <summary>
		/// Associates <paramref name="first"/> with the current available sprite index, and then increments the sprite index such that
		/// indexes [first+0] to [first+(sprites-1)] is able to be used for whatever this is.
		/// </summary>
		/// <param name="first">The property storing the first sprite's index. This can just be <see cref="OracleGraphics.totalSprites"/> to not bother setting anything.</param>
		/// <param name="amountOfSprites">The amount to increment totalSprites by.</param>
		private void AllocateSprites(ref int first, int amountOfSprites = 1) {
			first = totalSprites;
			totalSprites += amountOfSprites;
		}

		/// <summary>
		/// The same as <see cref="AllocateSprites(ref int, int)"/> but this will also assign an object and then use the provided
		/// function to get the amount of sprites allocated by that object (because there is no common interface declaring totalSprites on those objects).
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="first">The property storing the first sprite's index. This can just be <see cref="OracleGraphics.totalSprites"/> to not bother setting anything.</param>
		/// <param name="objectStorage">The field storing the object.</param>
		/// <param name="instance">The instance of object that was created.</param>
		/// <param name="getTotalSprites">A function to read the totalSprites property of the <typeparamref name="T"/>.</param>
		private void AllocateSpritesFromObject<T>(ref int first, ref T objectStorage, T instance, Func<T, int> getTotalSprites) {
			first = totalSprites;
			objectStorage = instance;
			totalSprites += getTotalSprites(instance);
		}

		#endregion


		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			Log.LogDebug("Initiating sprites...");
			// IMPORTANT SPECIAL BEHAVIOR:
			// To work around an issue with the lights, the sprites of Glass need to be put on a higher layer!
			// (The issue in question, future Xan, is that your transparent halo hologram VFX casted shadows)

			// The lights also need to be constructed here, and *MUST NOT* be declared in the room as objects
			// The rationale for this has to do with the fact that changing the layer could adversely affect other lights.
			// FIXME: Does this leak?
			_roomLights[0] = _roomLights[0] ?? new LightSource(new Vector2(205, 635), false, GLASS_AI_CHAMBER_CLR_IDLE_A, oracle);
			_roomLights[1] = _roomLights[1] ?? new LightSource(new Vector2(205, 105), false, GLASS_AI_CHAMBER_CLR_IDLE_B, oracle);
			_roomLights[2] = _roomLights[2] ?? new LightSource(new Vector2(735, 635), false, GLASS_AI_CHAMBER_CLR_IDLE_A, oracle);
			_roomLights[3] = _roomLights[3] ?? new LightSource(new Vector2(735, 105), false, GLASS_AI_CHAMBER_CLR_IDLE_B, oracle);

			for (int i = 0; i < _roomLights.Length; i++) {
				LightSource src = _roomLights[i];
				src.HardSetRad(625);
				src.HardSetAlpha(1.0f);
				
				rCam.room.AddObject(src);
			}

			sLeaser.sprites = new FSprite[totalSprites];
			for (int i = 0; i < owner.bodyChunks.Length; i++) {
				sLeaser.sprites[firstBodyChunkSprite + i] = new FSprite("Circle20", true);
				sLeaser.sprites[firstBodyChunkSprite + i].scale = owner.bodyChunks[i].rad / 10f;
				sLeaser.sprites[firstBodyChunkSprite + i].color = new Color(1f, (i == 0) ? 0.5f : 0f, (i == 0) ? 0.5f : 0f);
			}
			for (int j = 0; j < armJointGraphics.Length; j++) {
				armJointGraphics[j].InitiateSprites(sLeaser, rCam);
			}
			gown.InitiateSprite(robeSprite, sLeaser, rCam);
			halo.InitiateSprites(sLeaser, rCam);
			armBase.InitiateSprites(sLeaser, rCam);
			sLeaser.sprites[neckSprite] = new FSprite("pixel", true);
			sLeaser.sprites[neckSprite].scaleX = 4f;
			sLeaser.sprites[neckSprite].anchorY = 0f;
			sLeaser.sprites[HeadSprite] = new FSprite("Circle20", true);
			sLeaser.sprites[ChinSprite] = new FSprite("Circle20", true);
			sLeaser.sprites[HoopSprite] = new FSprite("LizardBubble5", true);
			sLeaser.sprites[HoopSprite].scaleX = 0.75f;
			sLeaser.sprites[HoopSprite].scaleY = 0.75f;
			for (int side = 0; side < 2; side++) {
				sLeaser.sprites[EyeSprite(side)] = new FSprite("pixel", true);
				sLeaser.sprites[PhoneSprite(side, 0)] = new FSprite("Circle20", true); // The base circle
				sLeaser.sprites[PhoneSprite(side, 1)] = new FSprite("FlyWing", true); // The first wing
				sLeaser.sprites[PhoneSprite(side, 2)] = new FSprite("FlyWing", true); // The second wing
				sLeaser.sprites[PhoneSprite(side, 1)].anchorY = 0f; // Was 0
				sLeaser.sprites[PhoneSprite(side, 1)].scaleY = 0.3333333f;//0.8f;
				sLeaser.sprites[PhoneSprite(side, 1)].scaleX = (side == 0) ? -1f : 1f;
				sLeaser.sprites[PhoneSprite(side, 2)].anchorY = 0f; // Was 0
				sLeaser.sprites[PhoneSprite(side, 2)].scaleY = 0.3333333f;//0.8f;
				sLeaser.sprites[PhoneSprite(side, 2)].scaleX = (side == 0) ? -1f : 1f;
				sLeaser.sprites[HandSprite(side, 0)] = new FSprite("haloGlyph-1", true);
				sLeaser.sprites[HandSprite(side, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
				sLeaser.sprites[FootSprite(side, 0)] = new FSprite("haloGlyph-1", true);
				sLeaser.sprites[FootSprite(side, 1)] = TriangleMesh.MakeLongMesh(7, false, true);
			}

			// TODO: Do I want a forehead symbol thingy?
			// ITERATOR CUTIE MARK ITERATOR CUTIE MARK ITERATOR CUTIE MARK
			// sLeaser.sprites[ForeheadBackgroundSprite] = new FSprite("Circle20", true);
			// sLeaser.sprites[ForeheadSymbolSprite] = new FSprite("mouseEyeA5", true); // Change this sprite, the eye is for SOS
			umbCord.InitiateSprites(sLeaser, rCam);

			sLeaser.sprites[HeadSprite].scaleX = head.rad / 9f;
			sLeaser.sprites[HeadSprite].scaleY = head.rad / 11f;
			sLeaser.sprites[ChinSprite].scale = head.rad / 15f;
			sLeaser.sprites[fadeSprite] = new FSprite("Futile_White", true);
			sLeaser.sprites[fadeSprite].scale = 12.5f;
			sLeaser.sprites[fadeSprite].color = new Color(1f, 1f, 1f);
			sLeaser.sprites[fadeSprite].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
			sLeaser.sprites[fadeSprite].alpha = 0.2f;

			// _spritesRef.SetTarget(sLeaser.sprites);
			AddToContainer(sLeaser, rCam, null);

			// base.InitiateSprites(sLeaser, rCam); // DO NOT CALL: Breaks stuff.
			// Do it manually, from GraphicsModule:
			DebugLabel[] dbgLabels = DEBUGLABELS;
			if (dbgLabels != null && dbgLabels.Length != 0) {
				foreach (DebugLabel debugLabel in dbgLabels) {
					rCam.ReturnFContainer("HUD").AddChild(debugLabel.label);
				}
			}
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			if (oracle == null || oracle.room == null) {
				return;
			}
			
			// TODO: If I plan to adapt this to public use I really, *really* want a matrix class so that
			// matrix transformations can be done rather than just doing trig. It'd be so, so much easier.
			
			Vector2 bodyPos = Vector2.Lerp(owner.firstChunk.lastPos, owner.firstChunk.pos, timeStacker);
			Vector2 bodyUp = Custom.DirVec(Vector2.Lerp(owner.bodyChunks[1].lastPos, owner.bodyChunks[1].pos, timeStacker), bodyPos);
			Vector2 bodyForward = Custom.PerpendicularVector(bodyUp);
			// Vector2 worldLookForward = Vector2.Lerp(lastLookDir, lookDir, timeStacker);
			Vector2 headPos = Vector2.Lerp(head.lastPos, head.pos, timeStacker);
			for (int i = 0; i < owner.bodyChunks.Length; i++) {
				sLeaser.sprites[firstBodyChunkSprite + i].x = Mathf.Lerp(owner.bodyChunks[i].lastPos.x, owner.bodyChunks[i].pos.x, timeStacker) - camPos.x;
				sLeaser.sprites[firstBodyChunkSprite + i].y = Mathf.Lerp(owner.bodyChunks[i].lastPos.y, owner.bodyChunks[i].pos.y, timeStacker) - camPos.y;
			}
			sLeaser.sprites[firstBodyChunkSprite].rotation = Custom.AimFromOneVectorToAnother(bodyPos, headPos) - Mathf.Lerp(14f, 0f, Mathf.Lerp(lastBreatheFac, breathFac, timeStacker));
			sLeaser.sprites[firstBodyChunkSprite + 1].rotation = Custom.VecToDeg(bodyUp);
			for (int j = 0; j < armJointGraphics.Length; j++) {
				armJointGraphics[j].DrawSprites(sLeaser, rCam, timeStacker, camPos);
			}
			sLeaser.sprites[fadeSprite].x = headPos.x - camPos.x;
			sLeaser.sprites[fadeSprite].y = headPos.y - camPos.y;
			sLeaser.sprites[neckSprite].x = bodyPos.x - camPos.x;
			sLeaser.sprites[neckSprite].y = bodyPos.y - camPos.y;
			sLeaser.sprites[neckSprite].rotation = Custom.AimFromOneVectorToAnother(bodyPos, headPos);
			sLeaser.sprites[neckSprite].scaleY = Vector2.Distance(bodyPos, headPos);
			gown.DrawSprite(robeSprite, sLeaser, rCam, timeStacker, camPos);
			halo.DrawSprites(sLeaser, rCam, timeStacker, camPos);
			armBase.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			Vector2 headToBody = Custom.DirVec(headPos, bodyPos);
			Vector2 neckForward = Custom.PerpendicularVector(headToBody);
			sLeaser.sprites[HeadSprite].x = headPos.x - camPos.x;
			sLeaser.sprites[HeadSprite].y = headPos.y - camPos.y;
			sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(headToBody);

			Vector2 relativeLookDirection = RelativeLookDir(timeStacker);
			sLeaser.sprites[HoopSprite].alpha = sLeaser.sprites[HeadSprite].alpha - 0.1f;

			float xSubOffset = relativeLookDirection.x * 2;
			float ySubOffset = (relativeLookDirection.y - 1.4f) * 2;
			float sidewaysFactor = 1f - Mathf.Abs(bodyUp.y); // 0 when upright, 1 when sideways.
			float effectiveX = Mathf.Lerp(xSubOffset, ySubOffset, sidewaysFactor);
			float effectiveY = Mathf.Lerp(ySubOffset, xSubOffset, sidewaysFactor);
			// Now these effective values need to be adjusted.
			// This will flip them depending on if the direction of the body is flipped on either axis.
			effectiveX *= bodyUp.x;
			effectiveY *= bodyUp.y;

			sLeaser.sprites[HoopSprite].x = sLeaser.sprites[HeadSprite].x - effectiveX;
			sLeaser.sprites[HoopSprite].y = sLeaser.sprites[HeadSprite].y - effectiveY;
			sLeaser.sprites[HoopSprite].scaleX = 0.75f * Mathf.Clamp01(1.6f - relativeLookDirection.y);
			//sLeaser.sprites[HoopSprite].scaleX = 0.8f - (Mathf.Abs(relativeLookDirection.y + 0.1f) * 0.3f);
			//sLeaser.sprites[HoopSprite].scaleY = 0.8f;
			float invSignDir = -Mathf.Sign(relativeLookDirection.x);
			float baseRot = (Mathf.Asin(relativeLookDirection.y * invSignDir) * Mathf.Rad2Deg) * 0.6f;
			//sLeaser.sprites[HoopSprite].anchorY = 0.75f * invSignDir;
			sLeaser.sprites[HoopSprite].rotation = baseRot - 90f;

			Vector2 chinPos = Vector2.Lerp(headPos, bodyPos, 0.15f);
			chinPos += neckForward * relativeLookDirection.x * 2f;
			sLeaser.sprites[ChinSprite].x = chinPos.x - camPos.x;
			sLeaser.sprites[ChinSprite].y = chinPos.y - camPos.y;
			float blink = Mathf.Lerp(lastEyesOpen, eyesOpen, timeStacker);
			for (int side = 0; side < 2; side++) {
				bool isLeft = side == 0;
				float signFromSide = isLeft ? -1f : 1f;
				int sideFacingScreen = (isLeft == (relativeLookDirection.x < 0f)) ? 0 : 1;

				Vector2 eyePositionBase = headPos + neckForward * Mathf.Clamp(relativeLookDirection.x * 3f + 2.5f * signFromSide, -5f, 5f) + headToBody * (1f - relativeLookDirection.y * 3f);
				sLeaser.sprites[EyeSprite(side)].rotation = Custom.VecToDeg(headToBody);
				sLeaser.sprites[EyeSprite(side)].scaleX = 1f + Mathf.InverseLerp(signFromSide, signFromSide * 0.5f, relativeLookDirection.x) + (1f - blink);
				sLeaser.sprites[EyeSprite(side)].scaleY = Mathf.Lerp(1f, 3f, blink);
				sLeaser.sprites[EyeSprite(side)].x = eyePositionBase.x - camPos.x;
				sLeaser.sprites[EyeSprite(side)].y = eyePositionBase.y - camPos.y;
				sLeaser.sprites[EyeSprite(side)].alpha = 0.5f + 0.5f * blink;
				Vector2 headphonePosBase = headPos + neckForward * Mathf.Clamp(Mathf.Lerp(7f, 5f, Mathf.Abs(relativeLookDirection.x)) * signFromSide, -11f, 11f);
				
				// Extracted from loop (there is only one circle instead of two)
				float headphoneX = headphonePosBase.x - camPos.x;
				float headphoneY = headphonePosBase.y - camPos.y;
				const float scaleFactor = 1f / 20f;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 0)].rotation = Custom.VecToDeg(headToBody);
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 0)].scaleY = 5.5f * scaleFactor;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 0)].scaleX = Mathf.Lerp(3.5f, 5f, Mathf.Abs(relativeLookDirection.x)) * scaleFactor;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 0)].x = headphoneX;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 0)].y = headphoneY;

				sLeaser.sprites[PhoneSprite(sideFacingScreen, 1)].x = headphoneX;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 1)].y = headphoneY;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 1)].scaleX = relativeLookDirection.x;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 1)].rotation = baseRot + 15f;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 2)].x = headphoneX;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 2)].y = headphoneY;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 2)].scaleX = relativeLookDirection.x;
				sLeaser.sprites[PhoneSprite(sideFacingScreen, 2)].rotation = baseRot - 35f;

				Vector2 handPos = Vector2.Lerp(hands[side].lastPos, hands[side].pos, timeStacker);
				Vector2 shoulderPos = bodyPos + bodyForward * -4f * signFromSide;
				Vector2 bezHandleB = handPos + Custom.DirVec(handPos, shoulderPos) * 3f + bodyUp;
				Vector2 bezHandleA = shoulderPos + bodyForward * -5f * signFromSide;
				sLeaser.sprites[HandSprite(side, 0)].x = handPos.x - camPos.x;
				sLeaser.sprites[HandSprite(side, 0)].y = handPos.y - camPos.y;
				Vector2 handAtSide = shoulderPos - bodyForward * -2f * signFromSide; // TODO: Is that where this is located?

				// To future Xan: The hand sprites are also inclusive of the arms.
				// The arms are divided into seven parts, which are interpolated below to allow the arms to bend to the hand.
				// HandSprite((L/R), (Arm/Hand))
				for (int iter = 0; iter < 7; iter++) {
					float progress = iter / 6f;
					Vector2 bezFromHandSideToUp = Custom.Bezier(shoulderPos, bezHandleA, handPos, bezHandleB, progress);
					Vector2 armDirectionUp = Custom.DirVec(handAtSide, bezFromHandSideToUp);
					Vector2 armForward = Custom.PerpendicularVector(armDirectionUp) * signFromSide;
					float handDist = Vector2.Distance(handAtSide, bezFromHandSideToUp);
					(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 0, bezFromHandSideToUp - armDirectionUp * handDist * 0.3f - armForward * 2f - camPos);
					(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 1, bezFromHandSideToUp - armDirectionUp * handDist * 0.3f + armForward * 2f - camPos);
					(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 2, bezFromHandSideToUp - armForward * 2f - camPos);
					(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 3, bezFromHandSideToUp + armForward * 2f - camPos);
					handAtSide = bezFromHandSideToUp;
				}
				Vector2 feetPos = Vector2.Lerp(feet[side].lastPos, feet[side].pos, timeStacker);
				Vector2 torsoPos = Vector2.Lerp(oracle.bodyChunks[1].lastPos, oracle.bodyChunks[1].pos, timeStacker);
				Vector2 kneePos = Vector2.Lerp(knees[side, 1], knees[side, 0], timeStacker);
				bezHandleB = Vector2.Lerp(feetPos, kneePos, 0.9f);
				bezHandleA = Vector2.Lerp(torsoPos, kneePos, 0.9f);
				sLeaser.sprites[FootSprite(side, 0)].x = feetPos.x - camPos.x;
				sLeaser.sprites[FootSprite(side, 0)].y = feetPos.y - camPos.y;
				Vector2 kneeIdle = torsoPos - bodyForward * -2f * signFromSide;
				float lastUnk0 = 4f;
				for (int iter = 0; iter < 7; iter++) {
					float progress = iter / 6f;
					float bendFactor = Mathf.Lerp(4f, 2f, Mathf.Sqrt(progress));
					Vector2 kneeHipToFootUp = Custom.Bezier(torsoPos, bezHandleA, feetPos, bezHandleB, progress);
					Vector2 kneeDirUp = Custom.DirVec(kneeIdle, kneeHipToFootUp);
					Vector2 kneeForward = Custom.PerpendicularVector(kneeDirUp) * signFromSide;
					float legDist = Vector2.Distance(kneeIdle, kneeHipToFootUp);
					(sLeaser.sprites[FootSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4, kneeHipToFootUp - kneeDirUp * legDist * 0.3f - kneeForward * (lastUnk0 + bendFactor) * 0.5f - camPos);
					(sLeaser.sprites[FootSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 1, kneeHipToFootUp - kneeDirUp * legDist * 0.3f + kneeForward * (lastUnk0 + bendFactor) * 0.5f - camPos);
					(sLeaser.sprites[FootSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 2, kneeHipToFootUp - kneeForward * bendFactor - camPos);
					(sLeaser.sprites[FootSprite(side, 1)] as TriangleMesh).MoveVertice(iter * 4 + 3, kneeHipToFootUp + kneeForward * bendFactor - camPos);
					kneeIdle = kneeHipToFootUp;
					lastUnk0 = bendFactor;
				}
			}
			// TODO: Use if I want a sigil.
			/*
			if (IsMoon || IsPastMoon || IsStraw) {
				Vector2 vector16 = interpolatedHeadPos + neckForward * relativeLookDirection.x * 2.5f + headToBody * (-2f - relativeLookDirection.y * 1.5f);
				sLeaser.sprites[MoonThirdEyeSprite].x = vector16.x - camPos.x;
				sLeaser.sprites[MoonThirdEyeSprite].y = vector16.y - camPos.y;
				sLeaser.sprites[MoonThirdEyeSprite].rotation = Custom.AimFromOneVectorToAnother(vector16, interpolatedHeadPos - headToBody * 10f);
				if (IsStraw) {
					sLeaser.sprites[MoonThirdEyeSprite].scaleX = Mathf.Lerp(0.8f, 0.6f, Mathf.Abs(relativeLookDirection.x));
					sLeaser.sprites[MoonThirdEyeSprite].scaleY = Custom.LerpMap(relativeLookDirection.y, 0f, 1f, 0.8f, 0.2f);
				} else {
					sLeaser.sprites[MoonThirdEyeSprite].scaleX = Mathf.Lerp(0.2f, 0.15f, Mathf.Abs(relativeLookDirection.x));
					sLeaser.sprites[MoonThirdEyeSprite].scaleY = Custom.LerpMap(relativeLookDirection.y, 0f, 1f, 0.2f, 0.05f);
				}
			}
			*/

			umbCord.DrawSprites(sLeaser, rCam, timeStacker, camPos);

			// Don't call base behavior here. It does not apply.
			// Do the operation manually from GraphicsModule
			DebugLabel[] dbgLabels = DEBUGLABELS;
			if (dbgLabels != null && dbgLabels.Length != 0) {
				foreach (DebugLabel debugLabel in dbgLabels) {
					if (debugLabel.relativePos) {
						debugLabel.label.x = owner.bodyChunks[0].pos.x + debugLabel.pos.x - camPos.x;
						debugLabel.label.y = owner.bodyChunks[0].pos.y + debugLabel.pos.y - camPos.y;
					} else {
						debugLabel.label.x = debugLabel.pos.x;
						debugLabel.label.y = debugLabel.pos.y;
					}
				}
			}

			ApplyChangesToWorkType(sLeaser);

			if (owner.slatedForDeletetion || owner.room != rCam.room || dispose) {
				sLeaser.CleanSpritesAndRemove();
			}

			if (sLeaser.sprites[0].isVisible == culled) {
				for (int j = 0; j < sLeaser.sprites.Length; j++) {
					sLeaser.sprites[j].isVisible = !culled;
				}
			}
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
			SLArmBaseColA = new Color(0.522f, 0.522f, 0.514f);
			SLArmHighLightColA = new Color(0.569f, 0.569f, 0.549f);
			SLArmBaseColB = palette.texture.GetPixel(5, 1);
			SLArmHighLightColB = palette.texture.GetPixel(5, 2);
			for (int i = 0; i < armJointGraphics.Length; i++) {
				armJointGraphics[i].ApplyPalette(sLeaser, rCam, palette);
			}

			Color bodyColor = GLASS_BODY_COLOR;
			for (int bodyChunk = 0; bodyChunk < owner.bodyChunks.Length; bodyChunk++) {
				sLeaser.sprites[firstBodyChunkSprite + bodyChunk].color = bodyColor;
			}
			sLeaser.sprites[HoopSprite].color = new Color(1.000f, 0.768f, 0.380f);
			sLeaser.sprites[neckSprite].color = bodyColor;
			sLeaser.sprites[HeadSprite].color = bodyColor;
			sLeaser.sprites[ChinSprite].color = bodyColor;
			for (int side = 0; side < 2; side++) {
				if (armJointGraphics.Length == 0) {
					sLeaser.sprites[PhoneSprite(side, 0)].color = GenericJointBaseColor();
					sLeaser.sprites[PhoneSprite(side, 1)].color = GenericJointHighLightColor();
					sLeaser.sprites[PhoneSprite(side, 2)].color = GenericJointHighLightColor();
				} else {
					sLeaser.sprites[PhoneSprite(side, 0)].color = armJointGraphics[0].BaseColor(default);
					sLeaser.sprites[PhoneSprite(side, 1)].color = armJointGraphics[0].HighLightColor(default);
					sLeaser.sprites[PhoneSprite(side, 2)].color = armJointGraphics[0].HighLightColor(default);
				}
				sLeaser.sprites[HandSprite(side, 0)].color = bodyColor;

				if (gown != null) {
					for (int vertex = 0; vertex < 7; vertex++) {
						(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).verticeColors[vertex * 4 + 0] = gown.Color(0.4f);
						(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).verticeColors[vertex * 4 + 1] = gown.Color(0f);
						(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).verticeColors[vertex * 4 + 2] = gown.Color(0.4f);
						(sLeaser.sprites[HandSprite(side, 1)] as TriangleMesh).verticeColors[vertex * 4 + 3] = gown.Color(0f);
					}
				} else {
					sLeaser.sprites[HandSprite(side, 1)].color = bodyColor;
				}
				sLeaser.sprites[FootSprite(side, 0)].color = bodyColor;
				sLeaser.sprites[FootSprite(side, 1)].color = bodyColor;
			}
			umbCord.ApplyPalette(sLeaser, rCam, palette);
			armBase.ApplyPalette(sLeaser, rCam, palette);
			sLeaser.sprites[firstUmbilicalSprite].color = palette.blackColor;
			ApplyChangesToWorkType(sLeaser);
		}

		/// <summary>
		/// Checks the current state of <see cref="GlassOracle.IsBusyProcessing"/> to determine if the current mode needs to change.
		/// </summary>
		/// <param name="sLeaser"></param>
		/// <param name="camera"></param>
		private void ApplyChangesToWorkType(RoomCamera.SpriteLeaser sLeaser) {
			bool? isProcessing = false;//oracle?.IsBusyProcessing;
			if (_lastKnownProcessingState == isProcessing) return;
			if (isProcessing == null) return;
			_lastKnownProcessingState = isProcessing;

			bool processing = isProcessing.Value;
			Color target = processing ? GLASS_SPARK_COLOR_BUSY : GLASS_SPARK_COLOR_IDLE;
			for (int i = 0; i < halo.totalSprites; i++) {
				int spriteIndex = halo.firstSprite + i;
				sLeaser.sprites[spriteIndex].color = target;
			}
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
			sLeaser.RemoveAllSpritesFromContainer();

			FContainer midground = newContatiner ?? rCam.ReturnFContainer("Midground");
			FContainer foreground = newContatiner ?? rCam.ReturnFContainer("Foreground");
			FContainer background = rCam.ReturnFContainer("Background");
			FContainer shortcuts = rCam.ReturnFContainer("Shortcuts");
			for (int i = 0; i < sLeaser.sprites.Length; i++) {
				FSprite sprite = sLeaser.sprites[i];
				bool renderedArm = false;
				bool renderedHalo = false;
				if (armBase != null) {
					if (i >= armBase.firstSprite && i < armBase.firstSprite + armBase.totalSprites) {
						renderedArm = true;
						if (i <= firstArmBaseSprite + 6 || i == firstArmBaseSprite + 8) {
							background.AddChild(sprite);
						} else {
							midground.AddChild(sprite);
						}
					}
				}
				if (halo != null) {
					if (i >= halo.firstSprite && i < halo.firstSprite + halo.totalSprites) {
						renderedHalo = true;
						if (i < 2) {
							// Vector circle
							background.AddChild(sprite);
						} else if (i >= halo.firstSprite + 2 && i < halo.firstSprite + halo.connections.Length + 2) {
							// Mycelium arc
							foreground.AddChild(sprite);
						} else {
							// Bits
							foreground.AddChild(sprite); // normally bg
						}
					}
				}
				if (!renderedArm && !renderedHalo) {
					// Body
					shortcuts.AddChild(sprite);
				}
			}
			shortcuts.AddChild(sLeaser.sprites[fadeSprite]);
			shortcuts.AddChild(sLeaser.sprites[killSprite]);
		}

		private bool _isEven = false;

		public override void Update() {
			base.Update();

			float target = 0.0f;
			//bool processing = false;
			if (_lastKnownProcessingState ?? false) {
				target = 1.0f;
				//processing = true;
			}
			_focusLevel = Mathf.Lerp(_focusLevel, target, Mathematical.RW_DELTA_TIME * 4);

			RoomCamera camera = WorldTools.CurrentCamera;
			if (camera.room == oracle.room && !camera.AboutToSwitchRoom && camera.paletteBlend != _focusLevel) {
				camera.ChangeBothPalettes(27, 55, _focusLevel);
			}

			Color clrALerp = Color.Lerp(GLASS_AI_CHAMBER_CLR_IDLE_A, GLASS_AI_CHAMBER_CLR_BUSY_A, _focusLevel);
			Color clrBLerp = Color.Lerp(GLASS_AI_CHAMBER_CLR_IDLE_B, GLASS_AI_CHAMBER_CLR_BUSY_B, _focusLevel);
			if (_roomLights[0] != null) {
				_roomLights[0].color = clrALerp;
				_roomLights[1].color = clrBLerp;
				_roomLights[2].color = clrALerp;
				_roomLights[3].color = clrBLerp;
			}

			for (int i = 0; i < _roomLights.Length; i++) {
				Color targetColor = i.IsOdd() ? clrBLerp : clrALerp;
				LightSource light = _roomLights[i];
				if (light == null) continue;
				_roomLights[i].color = targetColor;
				_roomLights[i].Update(_isEven);
			}
			_isEven = !_isEven;

			// This cameras[0] only works because AI is a single camera room.
			if (owner.room.game.cameras[0].AboutToSwitchRoom) {
				for (int i = 0; i < _roomLights.Length; i++) {
					_roomLights[i].Destroy();
					_roomLights[i] = null;
				}
			}
		}

		#region Halo

		private static void OnHaloUpdating(On.OracleGraphics.Halo.orig_Update originalMethod, OracleGraphics.Halo @this) {
			GlassOracleBehavior behavior = @this.owner.oracle.oracleBehavior as GlassOracleBehavior;
			if (behavior != null) {
				if (behavior.CurrentConnectionActivity >= 0) {
					@this.connectionsFireChance = behavior.CurrentConnectionActivity;
				}
			}
			originalMethod(@this);
		}

		private static void OnBuildHaloSprites(On.OracleGraphics.Halo.orig_InitiateSprites originalMethod, OracleGraphics.Halo @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			originalMethod(@this, sLeaser, rCam);

			if (@this.owner.oracle.ID == Oracles.GlassID) {
				rCam.room.lightAngle = Vector2.zero;
				int index;
				const int NUM_VECTOR_CIRCLES = 2;

				for (int i = 0; i < NUM_VECTOR_CIRCLES; i++) {
					// VECTOR CIRCLES
					index = @this.firstSprite + i;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.Shaders.AdditiveVertexColoredVectorCircle;
				}
				for (int i = 0; i < @this.connections.Length; i++) {
					// MYCELIA
					index = @this.firstSprite + i + NUM_VECTOR_CIRCLES;
					FSprite sprite = sLeaser.sprites[index];
					sprite.color = GLASS_SPARK_COLOR_IDLE;
					sprite.shader = XansAssets.Shaders.AdditiveVertexColored;
				}

				index = @this.firstBitSprite;
				for (int bitGroupIndex = 0; bitGroupIndex < @this.bits.Length; bitGroupIndex++) {
					// FILLED CHUNKS AROUND HALO
					OracleGraphics.Halo.MemoryBit[] bits = @this.bits[bitGroupIndex];
					for (int bitIndex = 0; bitIndex < bits.Length; bitIndex++) {
						FSprite sprite = sLeaser.sprites[index++];
						sprite.color = GLASS_SPARK_COLOR_IDLE;
						sprite.shader = XansAssets.Shaders.AdditiveVertexColored;
					}
				}
			}
		}
		private static void OnDrawHaloSprites(On.OracleGraphics.Halo.orig_DrawSprites originalMethod, OracleGraphics.Halo @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
			if (@this.owner.oracle.ID == Oracles.GlassID) {
				int spriteIndex;

				for (int connectionIndex = 0; connectionIndex < @this.connections.Length; connectionIndex++) {
					// MYCELIA
					spriteIndex = @this.firstSprite + connectionIndex + 2;
					FSprite sprite = sLeaser.sprites[spriteIndex];
					OracleGraphics.Halo.Connection connection = @this.connections[connectionIndex];
					if (connection.lightUp > 0.05f) {
						ClampVerticesIntoChamber(sprite, @this.owner.oracle.arm, camPos);
					}
				}

				for (int connectionIndex = 0; connectionIndex < @this.connections.Length; connectionIndex++) {
					// MYCELIA
					spriteIndex = @this.firstSprite + connectionIndex + 2;
					FSprite sprite = sLeaser.sprites[spriteIndex];
					OracleGraphics.Halo.Connection connection = @this.connections[connectionIndex];
					sprite.alpha = connection.lightUp <= 0.05f ? 0f : 1f; // This is okay, alpha controls its brightness.
				}

			}
		}

		#endregion

		#region Utils

		private static void ClampVerticesIntoChamber(Vector2[] vertices, Vector2 min, Vector2 max, Vector2 camPos) {
			float minX = (min.x - camPos.x) - 20;
			float maxX = (max.x - camPos.x) + 20;
			float minY = (min.y - camPos.y) - 20;
			float maxY = (max.y - camPos.y) + 20;

			for (int i = 0; i < vertices.Length; i++) {
				vertices[i] = new Vector2(Mathf.Clamp(vertices[i].x, minX, maxX), Mathf.Clamp(vertices[i].y, minY, maxY));
			}
		}

		private static void ClampVerticesIntoChamber(FSprite sprite, Oracle.OracleArm arm, Vector2 camPos) {
			if (sprite is TriangleMesh triangleMesh) {
				Vector2 min = arm.cornerPositions[3];
				Vector2 max = arm.cornerPositions[1];
				ClampVerticesIntoChamber(triangleMesh.vertices, min, max, camPos);
			}
		}

		#endregion

		#region Gown

		private static Color GetGownColor(On.OracleGraphics.Gown.orig_Color originalMethod, OracleGraphics.Gown @this, float f) {
			if (@this.owner.oracle.ID == Oracles.GlassID) {
				return Color.Lerp(GLASS_ROBE_COLOR_BASE, GLASS_ROBE_COLOR_HIGHLIGHT, f);
			}
			return originalMethod(@this, f);
		}

		#endregion
	}
}
