#ifndef __MECHUTILS_HACK_INCLUDED__
#define __MECHUTILS_HACK_INCLUDED__

#ifdef MECH_HOOK_UTILS_AVAILABLE
#define MECH_ONLY_HOOK(hookMethod, ...)									\
On.Player.hookMethod += (originalMethod, @this, ##__VA_ARGS__) => {		\
	if (@this.IsMechSlugcat()) {										\
		hookMethod(@this, ##__VA_ARGS__);								\
		return;															\
	}																	\
	originalMethod(@this, ##__VA_ARGS__);								\
}
#define MECH_ONLY_RETURNING_HOOK(hookMethod, ...)						\
On.Player.hookMethod += (originalMethod, @this, ##__VA_ARGS__) => {		\
	if (@this.IsMechSlugcat()) {										\
		return hookMethod(@this, ##__VA_ARGS__);						\
	}																	\
	return originalMethod(@this, ##__VA_ARGS__);						\
}
#endif

#endif