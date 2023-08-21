using DevInterface;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RegionKit.Modules.DevUIMisc.GenericNodes;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using XansCharacter.Data.Registry;
using XansCharacter.WorldObjects.Decorative;

namespace XansCharacter.WorldObjects {

	/// <summary>
	/// Adds additional behavior to <see cref="ZapCoil"/> such that <see cref="StableZapCoil"/> can work properly.
	/// </summary>
	public static class CustomObjectData {

		internal static void Initialize() {
			Log.LogMessage("Injecting into object registries...");
			Log.LogMessage("Injecting room loader to appropriately read serialized objects...");
			On.Room.Loaded += OnRoomLoaded;
			Log.LogMessage("Injecting object loader to create appropriate default data...");
			On.PlacedObject.GenerateEmptyData += OnGeneratingEmptyData;
			Log.LogMessage("Injecting dev tools mod code to draw editor widgets properly...");
			IL.DevInterface.ObjectsPage.CreateObjRep += InjectCreatingDevToolsObjRep;

			Log.LogMessage("Setting up static stable zap coil information...");
			Log.LogMessage("Injecting ZapCoil sprite drawer for custom sprite color...");
			On.ZapCoil.DrawSprites += OnDrawingSprites;
		}

		private static void InjectCreatingDevToolsObjRep(ILContext il) {
			Log.LogTrace("Attempting to inject into DevTools::CreateObjectRep");
			ILCursor cursor = new ILCursor(il);
			bool success = cursor.TryGotoNext(
				MoveType.After,
				instruction => instruction.MatchStloc(0),
				instruction => instruction.MatchLdloc(0),
				instruction => instruction.MatchBrfalse(out _)
			);
			success &= cursor.TryGotoNext( // Yes, this is technically redundant. The intent is to make it compatible with other mods in case some code is injected between these.
				MoveType.Before,
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchLdfld(out _),
				instruction => instruction.MatchLdloc(0),
				instruction => instruction.MatchCallvirt(out _),
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchLdfld(out _),
				instruction => instruction.MatchLdloc(0),
				instruction => instruction.MatchCallvirt(out _)
			);
			if (!success) throw new InvalidOperationException("Failed to inject into DevTools::CreateObjectRep");

			// The cursor should now be just before populating the data into the arrays.
			// What I want to do now is intercept this object and replace it on the fly iff it is a default PlacedObjectRepresentation

			cursor.Emit(OpCodes.Ldloca, 0); // Use ldloca instead of ldloc so that it can be passed by reference.
											// This will push the representation onto the stack.
											// Now mutate:
			cursor.EmitDelegate<ReplaceDelegate>(ReplacePlacedObjectRepresentationIfNeeded);
			Log.LogTrace("Injection succeeded.");
		}


		private delegate void ReplaceDelegate(ref PlacedObjectRepresentation rep);
		private static void ReplacePlacedObjectRepresentationIfNeeded(ref PlacedObjectRepresentation rep) {
			if (rep.GetType() == typeof(PlacedObjectRepresentation)) {
				Log.LogTrace("Intercepting PlacedObjectRepresentation...");
				// ^ DO NOT USE IS - This must be *EXACTLY EQUAL* to this type! Inheritence does not count!
				if (rep.pObj.type == PlaceableObjects.STABLE_ZAP_COIL) {
					Log.LogTrace($"Found {rep.pObj.type.value}...");
					rep.ClearSprites();
					rep = new ColoredGridRectObjectRepresentation(rep.owner, rep.pObj.ToString() + "_Rep", rep.parentNode, rep.pObj, PlaceableObjects.STABLE_ZAP_COIL.ToString(), "Stable Zap Coil");
					Log.LogTrace("Done appending this object's representation.");
				} else if (rep.pObj.type == PlaceableObjects.SUPERSTRUCTURE_VACUUM_TUBES) {
					Log.LogTrace($"Found {rep.pObj.type.value}...");
					rep.ClearSprites();
					rep = new SkinnedGridRectObjectRepresentation(rep.owner, rep.pObj.ToString() + "_Rep", rep.parentNode, rep.pObj, PlaceableObjects.SUPERSTRUCTURE_VACUUM_TUBES.ToString(), "Superstructure Vacuum Tubes");
					Log.LogTrace("Done appending this object's representation.");
				}
			}
		}

		private static void OnGeneratingEmptyData(On.PlacedObject.orig_GenerateEmptyData originalMethod, PlacedObject @this) {
			originalMethod(@this);
			if (@this.type == PlaceableObjects.STABLE_ZAP_COIL) {
				Log.LogTrace($"Generating empty data for {@this.type.value}...");
				@this.data = new ColoredGridRectObjectData(@this);
				Log.LogTrace($"Done (created {@this.data.GetType().FullName})");
			} else if (@this.type == PlaceableObjects.SUPERSTRUCTURE_VACUUM_TUBES) {
				Log.LogTrace($"Generating empty data for {@this.type.value}...");
				@this.data = new SkinnedGridRectObjectData(@this);
				Log.LogTrace($"Done (created {@this.data.GetType().FullName})");
			}
		}

		private static void OnRoomLoaded(On.Room.orig_Loaded originalMethod, Room @this) {
			originalMethod(@this);
			foreach (PlacedObject obj in @this.roomSettings.placedObjects) {
				// Log.LogTrace($"An instance of {obj.type.value} was loaded.");
				if (obj.type == PlaceableObjects.STABLE_ZAP_COIL) {
					ColoredGridRectObjectData data = obj.data as ColoredGridRectObjectData;
					@this.AddObject(new StableZapCoil(data.Rect, data, @this));
				} else if (obj.type == PlaceableObjects.SUPERSTRUCTURE_VACUUM_TUBES) {
					SkinnedGridRectObjectData data = obj.data as SkinnedGridRectObjectData;
					@this.AddObject(new SuperStructureVacuumTubes(obj, data, @this));
				}
				// Log.LogTrace($"Done");
			}
		}

		private static void OnDrawingSprites(On.ZapCoil.orig_DrawSprites originalMethod, ZapCoil @this, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos) {
			originalMethod(@this, sLeaser, rCam, timeStacker, camPos);
			if (@this is StableZapCoil stable) {
				if (!sLeaser.deleteMeNextFrame && stable.room == rCam.room) {
					Color modClr = stable.ElectricityColor;
					modClr.a = stable.EffectiveIntensity;
					sLeaser.sprites[0].color = modClr;
				}
			}
		}

		public class ColoredGridRectObjectData : PlacedObject.GridRectObjectData {

			public Color color;

			public ColoredGridRectObjectData(PlacedObject owner) : base(owner) {
				color = StableZapCoil.DEFAULT_COLOR;
			}

			public override void FromString(string s) {
				string[] array = Regex.Split(s, "~");
				handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.r = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.g = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.b = float.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture);
				color.a = float.Parse(array[5], NumberStyles.Any, CultureInfo.InvariantCulture);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 5);
			}

			public new string BaseSaveString() {
				return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}~{4}~{5}", handlePos.x, handlePos.y, color.r, color.g, color.b, color.a);
			}

			public override string ToString() {
				string baseString = BaseSaveString();
				baseString = SaveState.SetCustomData(this, baseString);
				return SaveUtils.AppendUnrecognizedStringAttrs(baseString, "~", unrecognizedAttributes);
			}
		}

		public class ColoredGridRectObjectRepresentation : GridRectObjectRepresentation {

			//private readonly FSprite _line;

			public ColoredGridRectObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name, string panelTitle) : base(owner, IDstring, parentNode, pObj, name) {
				ColoredGridRectObjectPanel panel = new ColoredGridRectObjectPanel(owner, "Colored_Grid_Rect_Panel", this, new Vector2(0f, 100f), new Vector2(250f, 95f), panelTitle);
				panel.pos = (pObj.data as ColoredGridRectObjectData).handlePos;
				subNodes.Add(panel);
				
				// TODO: Why does adding a line completely brick the widget?


				//_line = new FSprite("pixel", true);
				//_line.anchorY = 0;
				//fSprites.Add(_line);
				// owner.placedObjectsContainer.AddChild(_line);
			}

			public override void Refresh() {
				base.Refresh();
				//MoveSprite(6, absPos);

				//Panel panel = subNodes[0] as Panel;
				//_line.scaleY = panel.pos.magnitude;
				//_line.rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
			}

			public class ColoredGridRectObjectPanel : Panel {
				private readonly GenericSlider red, grn, blu, alp;

				private bool _hasSet = false;

				public ColoredGridRectObjectPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title) {
					Log.LogTrace("Constructing RGBA color sliders...");
					red = new GenericSlider(owner, "R_Slider", this, new Vector2(5f, 65f), "R: ", false, 110f, 255f, stringWidth: 24);
					grn = new GenericSlider(owner, "G_Slider", this, new Vector2(5f, 45f), "G: ", false, 110f, 255f, stringWidth: 24);
					blu = new GenericSlider(owner, "B_Slider", this, new Vector2(5f, 25f), "B: ", false, 110f, 255f, stringWidth: 24);
					alp = new GenericSlider(owner, "A_Slider", this, new Vector2(5f, 05f), "Intensity: ", false, 110f, 255f, stringWidth: 24);
					Log.LogTrace("Modifying data...");
					red.maxValue = 255f;
					grn.maxValue = 255f;
					blu.maxValue = 255f;
					alp.maxValue = 255f;
					Log.LogTrace("Registering sliders...");
					subNodes.Add(red);
					subNodes.Add(grn);
					subNodes.Add(blu);
					subNodes.Add(alp);
					Log.LogTrace("Construction complete.");
				}

				public override void Refresh() {
					base.Refresh();

					ColoredGridRectObjectRepresentation parent = parentNode as ColoredGridRectObjectRepresentation;
					if (parent is null) {
						Log.LogError($"Failed to acquire parentNode as ColoredGridRectObjectRepresentation. It is: {parentNode}");
						if (parentNode != null) {
							Log.LogError(parentNode.GetType().FullName);
						}
						return;
					}
					PlacedObject obj = parent.pObj;
					ColoredGridRectObjectData colorInfo = obj.data as ColoredGridRectObjectData;
					if (colorInfo is null) {
						Log.LogError($"Failed to acquire obj.data as ColoredGridRectObjectData. It is: {obj.data}");
						if (obj.data != null) {
							Log.LogError(obj.data.GetType().FullName);
						}
						return;
					}

					if (!_hasSet) {
						_hasSet = true;

						red.actualValue = colorInfo.color.r * 255f;
						grn.actualValue = colorInfo.color.g * 255f;
						blu.actualValue = colorInfo.color.b * 255f;
						alp.actualValue = colorInfo.color.a * 255f;
					}

					colorInfo.color = new Color(
						red.actualValue / 255f,
						grn.actualValue / 255f,
						blu.actualValue / 255f,
						alp.actualValue / 255f
					);
				}
			}
		}

		public class SkinnedGridRectObjectData : PlacedObject.GridRectObjectData {

			public int skin;

			public int animation;

			public SkinnedGridRectObjectData(PlacedObject owner) : base(owner) {
				skin = 0;
				animation = 0;
			}

			public override void FromString(string s) {
				string[] array = Regex.Split(s, "~");
				handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				skin = int.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
				animation = int.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 4);
			}
			public new string BaseSaveString() {
				return string.Format(CultureInfo.InvariantCulture, "{0}~{1}~{2}~{3}", handlePos.x, handlePos.y, skin, animation);
			}

			public override string ToString() {
				string baseString = BaseSaveString();
				baseString = SaveState.SetCustomData(this, baseString);
				return SaveUtils.AppendUnrecognizedStringAttrs(baseString, "~", unrecognizedAttributes);
			}

			
		}

		public class SkinnedGridRectObjectRepresentation : GridRectObjectRepresentation {
			public SkinnedGridRectObjectRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, string name, string panelTitle) : base(owner, IDstring, parentNode, pObj, name) {
				SkinnedGridRectObjectPanel panel = new SkinnedGridRectObjectPanel(owner, "Skinned_Grid_Rect_Panel", this, new Vector2(0f, 100f), new Vector2(250f, 55f), panelTitle);
				SkinnedGridRectObjectData info = (SkinnedGridRectObjectData)pObj.data;
				panel.pos = info.handlePos - new Vector2(info.Rect.Width * 10, info.Rect.Height * 10);
				subNodes.Add(panel);
			}

			public class SkinnedGridRectObjectPanel : Panel {
				private readonly GenericSlider _skin;
				private readonly GenericSlider _animation;

				private bool _hasSet = false;

				public SkinnedGridRectObjectPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, Vector2 size, string title) : base(owner, IDstring, parentNode, pos, size, title) {
					Log.LogTrace("Constructing RGBA color sliders...");
					_skin = new GenericSlider(owner, "Skin_Slider", this, new Vector2(5f, 25f), "Skin: ", false, 110f, 0, stringWidth: 24);
					_animation = new GenericSlider(owner, "Animation_Slider", this, new Vector2(5f, 05f), "Animation: ", false, 110f, 0, stringWidth: 24);
					Log.LogTrace("Modifying data...");
					_skin.maxValue = (int)SuperStructureVacuumTubes.SkinType.COUNT - 1;
					_animation.maxValue = (int)SuperStructureVacuumTubes.AnimationType.COUNT - 1;
					Log.LogTrace("Registering sliders...");
					subNodes.Add(_skin);
					subNodes.Add(_animation);
					Log.LogTrace("Construction complete.");
				}

				public override void Refresh() {
					base.Refresh();

					SkinnedGridRectObjectRepresentation parent = parentNode as SkinnedGridRectObjectRepresentation;
					if (parent is null) {
						Log.LogError($"Failed to acquire parentNode as ColoredGridRectObjectRepresentation. It is: {parentNode}");
						if (parentNode != null) {
							Log.LogError(parentNode.GetType().FullName);
						}
						return;
					}
					PlacedObject obj = parent.pObj;
					SkinnedGridRectObjectData displayInfo = obj.data as SkinnedGridRectObjectData;
					if (displayInfo is null) {
						Log.LogError($"Failed to acquire obj.data as ColoredGridRectObjectData. It is: {obj.data}");
						if (obj.data != null) {
							Log.LogError(obj.data.GetType().FullName);
						}
						return;
					}

					if (!_hasSet) {
						_hasSet = true;

						_skin.actualValue = displayInfo.skin;
						_animation.actualValue = displayInfo.animation;
					}

					displayInfo.skin = Mathf.FloorToInt(_skin.actualValue);
					displayInfo.animation = Mathf.FloorToInt(_animation.actualValue);
				}
			}


		}
	}
}
