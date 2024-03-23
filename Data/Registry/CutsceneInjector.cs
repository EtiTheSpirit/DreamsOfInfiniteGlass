using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DreamsOfInfiniteGlass.Data.Registry {

	/// <summary>
	/// Experimental, broken. Inject a custom cutscene.
	/// </summary>
	public sealed class CutsceneInjector {

		public static void Initialize() {
			// IL.Menu.SlideShow.ctor += OnConstructingSlideshow;
		}

		private static void OnConstructingSlideshow(ILContext il) {
			ILCursor cursor = new ILCursor(il);

			bool success = cursor.TryGotoNext(
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchLdarg(0),
				instruction => instruction.MatchLdfld(out _),
				instruction => instruction.MatchCallvirt(out _),
				instruction => instruction.MatchNewarr(out _)
			);

			if (!success) {
				throw new InvalidOperationException("Failed to find the desired slideshow code to patch.");
			}

			MethodInfo patchCutsceneMtd = typeof(CutsceneInjector).GetMethod(nameof(PatchCutscene), BindingFlags.Static | BindingFlags.NonPublic);

			cursor.Emit(OpCodes.Ldarg_0);
			cursor.Emit(OpCodes.Ldarg_1);
			cursor.Emit(OpCodes.Ldarg_2);
			cursor.EmitDelegate<PatchCutsceneDelegate>(PatchCutscene);
		}

		private delegate void PatchCutsceneDelegate(SlideShow @this, ProcessManager manager, SlideShow.SlideShowID id);

		private static void PatchCutscene(SlideShow @this, ProcessManager manager, SlideShow.SlideShowID id) {
			// if (id != yourId) return;
			@this.playList.Clear();
			@this.playList.Add(new SlideShow.Scene(MoreSlugcatsEnums.MenuSceneID.Intro_S1, 0, 10, 50));
			@this.waitForMusic = "BM_SS_DOOR";
			@this.stall = true;
			manager.musicPlayer.MenuRequestsSong(@this.waitForMusic, 1.5f, 10f);
			@this.processAfterSlideShow = ProcessManager.ProcessID.MainMenu;
			// Do your setup here
		}

	}
}
