using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DreamsOfInfiniteGlass.Data.Creature {
	public interface IProvideUpdatesAndGraphics : IDrawable {

		void UpdateObject(bool eu);

		void UpdateGraphics();

		void ResetObject();

		void ResetGraphics();

	}
}
