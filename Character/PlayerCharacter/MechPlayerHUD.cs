using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Character.PlayerCharacter.DataStorage;
using XansCharacter.LoadedAssets;
using XansTools.Utilities;

namespace XansCharacter.Character.PlayerCharacter {
	public class MechPlayerHUD : HudPart {

		public MechPlayerData Player { get; }

		private FContainer HUD2 => hud.fContainers[1];

		private FSprite _batteryMeter;

		private float _batteryChargePhase;
		private float _batteryChargeIntensity;
		private float _batterySapPhase;
		private float _batterySapIntensity;

		public MechPlayerHUD(MechPlayerData player, HUD.HUD parent) : base(parent) {
			Player = player;
			_batteryMeter = XansAssets.Sprites.BatteryHudMask.CreateNew();
			_batteryMeter.shader = XansAssets.Shaders.SpecialBatteryMeterShader;
			HUD2.AddChild(_batteryMeter);
		}
		public override void ClearSprites() {
			HUD2.RemoveChild(_batteryMeter);
			_batteryMeter = null;
		}

		public override void Update() {
			Material batteryMtl = _batteryMeter._renderLayer._material;
			batteryMtl.SetFloat("_Value", Player.GetClampedBatteryCharge() * 0.01f);
			if (Player.BatteryState < 0) {
				_batteryChargeIntensity = Mathf.Clamp01(_batteryChargeIntensity - Mathematical.RW_DELTA_TIME);
				_batterySapIntensity = Mathf.Clamp01(_batterySapIntensity + Mathematical.RW_DELTA_TIME);
			} else if (Player.BatteryState > 0) {
				_batteryChargeIntensity = Mathf.Clamp01(_batteryChargeIntensity + Mathematical.RW_DELTA_TIME);
				_batterySapIntensity = Mathf.Clamp01(_batterySapIntensity - Mathematical.RW_DELTA_TIME);
			} else {
				_batteryChargeIntensity = Mathf.Clamp01(_batteryChargeIntensity - Mathematical.RW_DELTA_TIME);
				_batterySapIntensity = Mathf.Clamp01(_batterySapIntensity - Mathematical.RW_DELTA_TIME);
			}

			_batteryChargePhase += Mathematical.RW_DELTA_TIME * 1.2f;
			_batterySapPhase += Mathematical.RW_DELTA_TIME * 1.2f;
		}

		private static float Trig01(Func<float, float> trig, float phase, float intensity) {
			return ((trig(phase) + 1.0f) * 0.5f) * intensity;
		}

		public override void Draw(float timeStacker) {
			_batteryMeter.x = Futile.screen.pixelWidth - 130;
			_batteryMeter.y = Futile.screen.pixelHeight - 66;
			Material batteryMtl = _batteryMeter._renderLayer._material;
			Color clr = Color.Lerp(
				Color.red,
				Color.green,
				Player.GetClampedBatteryCharge() * 0.01f
			);
			clr = Color.Lerp(clr, Color.magenta, Trig01(Mathf.Sin, _batterySapPhase, _batterySapIntensity));
			clr = Color.Lerp(clr, Color.cyan, Trig01(Mathf.Cos, _batteryChargePhase, _batteryChargeIntensity));
			batteryMtl.SetColor("_Color", clr);
		}
	}
}
