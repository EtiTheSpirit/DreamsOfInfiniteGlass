using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XansCharacter.WorldObjects {

	/// <summary>
	/// A <see cref="LightSource"/> that can be put on any layer.
	/// </summary>
	[Obsolete]
	public class CustomLayerLightSource : LightSource {

		public string Layer {
			get => _layer;
			set {
				if (_layer == value) return;
				
			}
		}
		private string _layer = "ForegroundLights";

		[Obsolete("When referencing a CustomLayerLightSource, use Layer instead of LayerName.", true)]
		public override string LayerName => Layer;

		public CustomLayerLightSource(Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject) : this(initPos, environmentalLight, color, tiedToObject, false) { }

		public CustomLayerLightSource(Vector2 initPos, bool environmentalLight, Color color, UpdatableAndDeletable tiedToObject, bool submersible) : base(initPos, environmentalLight, color, tiedToObject, submersible) { }

	}
}
