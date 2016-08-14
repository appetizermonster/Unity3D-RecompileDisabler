using System.Collections;
using UnityEditor;
using UnityEngine;

namespace RecompileDisabler {

	public sealed class RecompileDisablerPreference {
		private static bool prefsLoaded_ = false;
		private static bool disableRecompile_ = false;

		[PreferenceItem("Recompile\n       Disabler")]
		public static void PreferencesGUI () {
			if (!prefsLoaded_) {
				LoadPrefs();
				prefsLoaded_ = true;
			}

			EditorGUI.BeginChangeCheck();
			disableRecompile_ = EditorGUILayout.Toggle("Disable Recompile on Play mode", disableRecompile_);

			// Save if changed
			if (EditorGUI.EndChangeCheck())
				EditorPrefs.SetBool(RecompileDisabler.DISABLE_RECOMPILE_PREF_KEY, disableRecompile_);
		}

		private static void LoadPrefs () {
			disableRecompile_ = EditorPrefs.GetBool(RecompileDisabler.DISABLE_RECOMPILE_PREF_KEY, true);
		}
	}
}
