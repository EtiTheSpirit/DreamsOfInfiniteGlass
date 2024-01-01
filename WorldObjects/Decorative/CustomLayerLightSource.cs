#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Character.NPC.Iterator;
using DreamsOfInfiniteGlass.Character.NPC.Iterator.Graphics;
using XansTools.Utilities.General;

namespace DreamsOfInfiniteGlass.WorldObjects {

	/// <summary>
	/// A <see cref="LightSource"/> that can be put on any layer.
	/// </summary>
	[Obsolete("Re-evaluate if this needs to exist.")]
	public class CustomLayerLightSource : Extensible.LightSource {

		public string Layer {
			get => _layer;
			set {
				if (_layer == value) return;
				_layer = value;
			}
		}
		private string _layer = "ForegroundLights";

		public override string LayerName => _layer;

		public string VanillaLayerName => base.LayerName;

		internal static void Initialize() {
			Log.LogDebug("Creating extensible light source type...");
			On.LightSource.ctor_Vector2_bool_Color_UpdatableAndDeletable += (originalMethod, @this, initPos, environmentalLight, color, tiedToObject) => {
				originalMethod(@this, initPos, environmentalLight, color, tiedToObject);
				if (tiedToObject is Oracle oracle && Extensible.Oracle.Binder<GlassOracle>.TryGetBinding(oracle, out _)) {
					Binder<CustomLayerLightSource>.Bind(@this);
				}
			};
		}

		public static CustomLayerLightSource GetLight(GlassOracleGraphics glass, Vector2 at, bool isEnvironmental, Color color) {
			LightSource light = new LightSource(at, isEnvironmental, color, glass.oracle);
			Binder<CustomLayerLightSource>.TryGetBinding(light, out CustomLayerLightSource realLight);
			return realLight;
		}

		CustomLayerLightSource(LightSource original) : base(original) { }

	}
}
