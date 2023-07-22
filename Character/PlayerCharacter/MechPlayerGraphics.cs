using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XansCharacter.Character.PlayerCharacter {

	/// <summary>
	/// Overrides the default player graphics module for use with the mechanical slugcat.
	/// </summary>
	public class MechPlayerGraphics : PlayerGraphics {

		public static Color CHASSIS_COLOR = new Color(0.5f, 0.5f, 0.5f);
		

		public MechPlayerGraphics(PhysicalObject ow) : base(ow) {
			// Theoretically it is possible to use a hook to cancel the base() ctor call...
			// Is this a good idea?
		}

		#region Drawable

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) {
			base.InitiateSprites(sLeaser, rCam);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
		}

		public override void Update() {
			base.Update();
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
			base.AddToContainer(sLeaser, rCam, newContatiner);
		}

		public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette) {
			base.ApplyPalette(sLeaser, rCam, palette);
		}

		#endregion



		public override void SuckedIntoShortCut(Vector2 shortCutPosition) {
			base.SuckedIntoShortCut(shortCutPosition);
		}

		public override void PushOutOf(Vector2 pos, float rad) {
			base.PushOutOf(pos, rad);
		}

	}
}
