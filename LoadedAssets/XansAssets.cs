using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;
using XansTools.AssetSystems;

namespace XansCharacter.LoadedAssets {

	public static class XansAssets {

		/// <summary>
		/// This shader applies additively. It is best used above the light layer, as it may cast shadows otherwise.
		/// <para/>
		/// Textures: N/A<br/>
		/// UVs: N/A<br/>
		/// Vertex Colors: color.rgb, (alpha serves as intensity)
		/// </summary>
		public static FShader AdditiveVertexColored { get; private set; }

		/// <summary>
		/// This shader applies additively. It is best used above the light layer, as it may cast shadows otherwise.
		/// <para/>
		/// Functionally this mimics the VectorCircle shader that comes with RW. Vertex color controls its color, and the alpha channel controls its radius as a %.
		/// <para/>
		/// Textures: N/A<br/>
		/// UVs: N/A<br/>
		/// Vertex Colors: color.rgb * color.a (color.a determines the inside radius)
		/// </summary>
		public static FShader AdditiveVertexColoredVectorCircle { get; private set; }

		/// <summary>
		/// An alternate LevelColor shader that supports additional features leveraged by this mod.
		/// </summary>
		public static FShader SpecializedLevelShader { get; private set; }

		internal static void Initialize() {
			On.RainWorld.LoadResources += OnLoadingResources;
		}

		private static void OnLoadingResources(On.RainWorld.orig_LoadResources originalMethod, RainWorld @this) {
			originalMethod(@this);
			try {
				Log.LogMessage("Loading assets...");
				AssetBundle bundle = AssetLoader.LoadAssetBundleFromEmbeddedResource("XansCharacter.assets.embedded.xansshaders");

				Log.LogTrace("Loading shaders...");
				AdditiveVertexColored = bundle.FindFShader("Dreams of Infinite Glass/Futile/Additive (Color)");
				AdditiveVertexColoredVectorCircle = bundle.FindFShader("Dreams of Infinite Glass/Futile/Additive Vector Circle (Color)");
				SpecializedLevelShader = bundle.FindFShader("Dreams of Infinite Glass/Futile/Enhanced Level Color");

				Log.LogTrace("Shaders have been loaded!");
			} catch (Exception error) {
				DreamsOfInfiniteGlassPlugin.Reporter.DeferredReportModInitError(error, "Loading custom shaders and embedded assets.");
				throw;
			} finally {
				Log.LogTrace("Disposing of this hook, as it is no longer needed...");
				On.RainWorld.LoadResources -= OnLoadingResources;
			}
			
		}

		#region Legacy API

		[Obsolete]
		private static AssetBundle LoadFromEmbeddedResource(string fullyQualifiedPath) {
			Log.LogMessage($"Loading embedded asset bundle: {fullyQualifiedPath}");
			using (MemoryStream mstr = new MemoryStream()) {
				Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullyQualifiedPath);
				str.CopyTo(mstr);
				str.Flush();
				str.Close();
				Log.LogTrace("Bundle loaded into memory as byte[], processing with Unity...");
				AssetBundle bundle = AssetBundle.LoadFromMemory(mstr.ToArray());
				Log.LogTrace("Unity has successfully loaded this asset bundle from memory.");
				return bundle;
			}
		}

		[Obsolete]
		private static FShader CreateFromAsset(AssetBundle bundle, string shortName) {
			Log.LogTrace($"Loading shader \"{shortName}\"...");
			Shader target = bundle.LoadAsset<Shader>($"assets/{shortName}.shader");
			Log.LogTrace($"Implementing shader \"{shortName}\" into Futile...");
			return FShader.CreateShader(shortName, target);
		}

		#endregion
	}
} 
