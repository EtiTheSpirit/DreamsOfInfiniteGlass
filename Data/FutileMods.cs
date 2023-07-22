using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Experimental.Rendering;

namespace XansCharacter.Data {
	public static class FutileMods {

		internal static void Initialize() {
			Log.LogMessage("Retrofitting a (nonfunctional) Depth Buffer and a (very functional) Stencil Buffer into Futile...");
			On.FScreen.ctor += OnConstructingFScreen;
			On.FScreen.ReinitRenderTexture += OnReinitializeRT;
		}

		private static void OnReinitializeRT(On.FScreen.orig_ReinitRenderTexture originalMethod, FScreen @this, int displayWidth) {
			originalMethod(@this, displayWidth);
			@this.renderTexture.depth = 24; // 16 bit depth buffer + 8 bit stencil buffer. 24 is a value that Unity sets when you choose this option in editor.
		}

		private static void OnConstructingFScreen(On.FScreen.orig_ctor originalCtor, FScreen @this, FutileParams futileParams) {
			originalCtor(@this, futileParams);
			@this.renderTexture.depth = 24; // 16 bit depth buffer + 8 bit stencil buffer. 24 is a value that Unity sets when you choose this option in editor.
		}
	}
}
