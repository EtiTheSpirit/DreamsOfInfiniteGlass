using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.WorldObjects.Decorative {
	public class SuperpositionMatrixEffect : UpdatableAndDeletable, IDrawable {

		public override void Update(bool eu) {
			base.Update(eu);

			
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

		public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner) {
			throw new NotImplementedException();
		}
	}
}
