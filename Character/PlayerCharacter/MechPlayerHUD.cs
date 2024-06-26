﻿#nullable enable
using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DreamsOfInfiniteGlass.Character.PlayerCharacter.DataStorage;
using DreamsOfInfiniteGlass.LoadedAssets;
using XansTools.Utilities;

namespace DreamsOfInfiniteGlass.Character.PlayerCharacter {
	public class MechPlayerHUD : HudPart {

		public MechPlayer Player { get; }

		private FContainer HUD2 => hud.fContainers[1];

		private FSprite? _batteryMeter;

		private static readonly MaterialPropertyBlock PROPS = new MaterialPropertyBlock();

		private float _batteryChargePhase;
		private float _batteryChargeIntensity;
		private float _batterySapPhase;
		private float _batterySapIntensity;

		public MechPlayerHUD(MechPlayer player, HUD.HUD parent) : base(parent) {
			Player = player;
			_batteryMeter = XansAssets.Sprites.BatteryHudMask.CreateNew();
			_batteryMeter.shader = XansAssets.Shaders.SpecialBatteryMeterShader;
			HUD2.AddChild(_batteryMeter);
		}

		public override void ClearSprites() {
			base.ClearSprites();
			HUD2.RemoveChild(_batteryMeter);
			_batteryMeter = null;
		}

		public override void Update() {
			base.Update();
			if (_batteryMeter == null) return;
			Renderer renderer = _batteryMeter._renderLayer._meshRenderer;
			renderer.GetPropertyBlock(PROPS);
			PROPS.SetFloat("_Value", Player.Battery.ClampedCharge * 0.01f);
			renderer.SetPropertyBlock(PROPS);

			if (Player.Battery.IsDraining) {
				_batteryChargeIntensity = Mathf.Clamp01(_batteryChargeIntensity - Mathematical.RW_DELTA_TIME);
				_batterySapIntensity = Mathf.Clamp01(_batterySapIntensity + Mathematical.RW_DELTA_TIME);
			} else if (Player.Battery.IsCharging) {
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
			base.Draw(timeStacker);
			if (_batteryMeter == null) return;
			_batteryMeter.x = Futile.screen.pixelWidth - 130;
			_batteryMeter.y = Futile.screen.pixelHeight - 66;
			Color clr = Color.Lerp(
				Color.red,
				Color.green,
				Player.Battery.ClampedCharge * 0.01f
			);
			clr = Color.Lerp(clr, Color.magenta, Trig01(Mathf.Sin, _batterySapPhase, _batterySapIntensity));
			clr = Color.Lerp(clr, Color.cyan, Trig01(Mathf.Cos, _batteryChargePhase, _batteryChargeIntensity));

			Renderer renderer = _batteryMeter._renderLayer._meshRenderer;
			renderer.GetPropertyBlock(PROPS);
			PROPS.SetColor("_Color", clr);
			renderer.SetPropertyBlock(PROPS);
		}
	}
}
