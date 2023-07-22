using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XansCharacter.LoadedAssets {

	public static class XansAssets {

		/// <summary>
		/// This shader applies additively on the transparent queue.
		/// <para/>
		/// Textures: N/A<br/>
		/// UVs: N/A<br/>
		/// Vertex Colors: color.rgb, (alpha serves as intensity)
		/// </summary>
		public static FShader XAdditiveVtxClr { get; private set; }

		/// <summary>
		/// The same as <see cref="XAdditiveVtxClr"/> but the alpha channel does nothing.
		/// </summary>
		public static FShader XAdditiveVtxClrNA { get; private set; }

		/// <summary>
		/// This shader applies additively on the transparent queue.
		/// <para/>
		/// Functionally this mimics the VectorCircle shader that comes with RW. Vertex color controls its color, and the alpha channel controls its radius as a %.
		/// <para/>
		/// Textures: N/A<br/>
		/// UVs: N/A<br/>
		/// Vertex Colors: color.rgb * color.a (color.a determines the inside radius)
		/// </summary>
		public static FShader XAdditiveVectorCircle { get; private set; }

		/// <summary>
		/// This shader has been hardcoded for the express and singular purpose of serving as the effect to be used on the iterator's halo.
		/// <para/>
		/// It probably won't work well anywhere else.
		/// </summary>
		public static FShader GlassHaloEffects { get; private set; }

		/// <summary>
		/// This shader has been hardcoded for the express and singular purpose of serving as the effect to be used on the iterator's halo.
		/// <para/>
		/// It probably won't work well anywhere else.
		/// </summary>
		public static FShader GlassHaloEffectsCircle { get; private set; }

		// <summary>
		// This is a white additive shader that uses the stencil buffer to prevent overlapped areas from combining together.
		// </summary>
		//public static FShader AdditiveStencilTest { get; private set; }

		internal static void Initialize() {
			On.RainWorld.LoadResources += OnLoadingResources;
		}

		/// <summary>
		/// Given the name of a file marked as "Embedded Resource" in the VS solution, 
		/// </summary>
		/// <param name="fileName"></param>
		/// <returns></returns>
		private static AssetBundle LoadFromEmbeddedResource(string fileName) {
			Log.LogMessage($"Loading embedded asset bundle: {fileName}");
			using (MemoryStream mstr = new MemoryStream()) {
				Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream($"XansCharacter.assets.{fileName}");
				str.CopyTo(mstr);
				str.Flush();
				str.Close();
				Log.LogTrace("Bundle loaded into memory as byte[], processing with Unity...");
				AssetBundle bundle = AssetBundle.LoadFromMemory(mstr.ToArray());
				Log.LogTrace("Unity has successfully loaded this asset bundle from memory.");
				return bundle;
			}
		}

		private static FShader CreateFromAsset(AssetBundle bundle, string shortName) {
			Log.LogTrace($"Loading shader \"{shortName}\"...");
			Shader target = bundle.LoadAsset<Shader>($"assets/{shortName}.shader");
			Log.LogTrace($"Implementing shader \"{shortName}\" into Futile...");
			return FShader.CreateShader(shortName, target);
		}

		private static void OnLoadingResources(On.RainWorld.orig_LoadResources originalMethod, RainWorld @this) {
			originalMethod(@this);
			try {
				Log.LogMessage("Loading assets...");
				AssetBundle bundle = LoadFromEmbeddedResource("xansshaders");

				Log.LogTrace("Loading shaders...");
				XAdditiveVtxClr = CreateFromAsset(bundle, "NativeAdditiveColor");
				XAdditiveVtxClrNA = CreateFromAsset(bundle, "NativeAdditiveColorNoAlpha");
				XAdditiveVectorCircle = CreateFromAsset(bundle, "NativeAdditiveVectorCircle");
				GlassHaloEffects = CreateFromAsset(bundle, "GlassHaloFX");
				GlassHaloEffectsCircle = CreateFromAsset(bundle, "GlassHaloFXCircle");

				Log.LogTrace("Shaders have been loaded!");
			} catch (Exception error) {
				Log.LogFatal("WAKE THE FUCK UP SAMURAI. I SHIT THE BED.");
				Log.LogFatal(error);

			} finally {
				Log.LogTrace("Disposing of this hook, as it is no longer needed...");
				On.RainWorld.LoadResources -= OnLoadingResources;
			}
			
		}
	}
} 
