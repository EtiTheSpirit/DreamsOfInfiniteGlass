#nullable enable
using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using XansTools.AssetSystems;
using XansTools.Utilities.RW;

namespace DreamsOfInfiniteGlass.LoadedAssets {

	public static class XansAssets {


		internal static void Initialize() {
			On.RainWorld.PostModsInit += OnPostModsInit;
		}

		private static void OnPostModsInit(On.RainWorld.orig_PostModsInit originalMethod, RainWorld @this) {
			originalMethod(@this);
			try {
				Log.LogMessage("Loading assets...");
				AssetBundle bundle = AssetLoader.LoadAssetBundleFromEmbeddedResource("DreamsOfInfiniteGlass.assets.embedded.xansshaders");

				Log.LogDebug("Loading shaders...");
				Shaders.AdditiveVertexColored = bundle.FindFShader("Dreams of Infinite Glass/Futile/Additive (Color)");
				Shaders.AdditiveVertexColoredVectorCircle = bundle.FindFShader("Dreams of Infinite Glass/Futile/Additive Vector Circle (Color)");
				Shaders.SpecializedLevelShader = bundle.FindFShader("Dreams of Infinite Glass/Futile/Enhanced Level Color");
				Shaders.SpecialBatteryMeterShader = bundle.FindFShader("Dreams of Infinite Glass/HUD/Meter");

				Log.LogDebug("Loading images...");
				//Sprites.BatteryHudMask = new Sprites.SpriteProvider("batterybar_mask.png");

				Log.LogDebug("Loading complete!");
			} catch (Exception error) {
				DreamsOfInfiniteGlassPlugin.Reporter.DeferredReportModInitError(error, "Loading custom shaders and embedded assets.");
				throw;
			} finally {
				Log.LogTrace("Disposing of this hook, as it is no longer needed...");
				On.RainWorld.PostModsInit -= OnPostModsInit;
			}
		}

		public static class Shaders {

			/// <summary>
			/// This shader applies additively. It is best used above the light layer, as it may cast shadows otherwise.
			/// <para/>
			/// Textures: N/A<br/>
			/// UVs: N/A<br/>
			/// Vertex Colors: color.rgb, (alpha serves as intensity)
			/// </summary>
			[AllowNull]
			public static FShader AdditiveVertexColored { get; internal set; }

			/// <summary>
			/// This shader applies additively. It is best used above the light layer, as it may cast shadows otherwise.
			/// <para/>
			/// Functionally this mimics the VectorCircle shader that comes with RW. Vertex color controls its color, and the alpha channel controls its radius as a %.
			/// <para/>
			/// Textures: N/A<br/>
			/// UVs: N/A<br/>
			/// Vertex Colors: color.rgb * color.a (color.a determines the inside radius)
			/// </summary>
			[AllowNull]
			public static FShader AdditiveVertexColoredVectorCircle { get; internal set; }

			/// <summary>
			/// An alternate LevelColor shader that supports additional features leveraged by this mod.
			/// </summary>
			[AllowNull]
			public static FShader SpecializedLevelShader { get; internal set; }

			/// <summary>
			/// The shader used on the battery HUD element.
			/// </summary>
			[AllowNull]
			public static FShader SpecialBatteryMeterShader { get; internal set; }

		}

		public static class Sprites {

			/// <summary>
			/// Allows creating sprites for the battery HUD mask. Its resolution is 128x32
			/// </summary>
			[AllowNull]
			public static SpriteProvider BatteryHudMask { get; internal set; }

			public class SpriteProvider {

				/// <summary>
				/// A single-image atlas containing the loaded sprite image.
				/// </summary>
				public FAtlas Atlas { get; }

				/// <summary>
				/// A reference to the underlying <see cref="FAtlasElement"/> describing the image itself.
				/// </summary>
				public FAtlasElement Element { get; }

				/// <summary>
				/// The name of this sprite's file.
				/// </summary>
				public string FileName { get; }

				/// <summary>
				/// Constructs a new <see cref="FSprite"/> using the data of this provider.
				/// </summary>
				/// <returns></returns>
				public FSprite CreateNew() {
					return new FSprite(Element);
				}

				private static string GetHUDElementPath(string subdirectory, string name) {
					for (int num = ModManager.ActiveMods.Count - 1; num >= 0; num--) {
						Log.LogTrace(ModManager.ActiveMods[num].path);
					}
					return AssetManager.ResolveFilePath(Path.Combine(subdirectory, name));
				}

				/// <summary>
				/// Create a new <see cref="SpriteProvider"/> from the given image file name.
				/// </summary>
				/// <param name="imageName">The name of the image file, <strong>excluding</strong> its .png extension.</param>
				/// <param name="subdirectory">The subdirectory of the mod folder that this image is located in. This can be a path too, for nested folders.</param>
				public SpriteProvider(string imageName, string subdirectory = "hud") {
					FileName = imageName;
					Atlas = Futile.atlasManager.LoadImage(GetHUDElementPath(subdirectory, imageName));
					Element = Atlas.elements[0];
				}

			}
		}

	}
} 
