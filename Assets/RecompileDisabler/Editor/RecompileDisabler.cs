using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using Diag = System.Diagnostics;

namespace RecompileDisabler {

	[InitializeOnLoad]
	public static class RecompileDisabler {
		public const string DISABLE_RECOMPILE_PREF_KEY = "RecompileDisabler_disableRecompile";

		private static bool lastPlaying_ = false;

		static RecompileDisabler () {
			EditorApplication.playmodeStateChanged += OnEditorPlayModeStateChanged;
			lastPlaying_ = EditorApplication.isPlaying;
		}

		private static void OnEditorPlayModeStateChanged () {
			// Skip if state will be changed soon
			if (EditorApplication.isPlaying != EditorApplication.isPlayingOrWillChangePlaymode)
				return;
			if (EditorApplication.isPlaying == lastPlaying_)
				return;
			lastPlaying_ = EditorApplication.isPlaying;

			if (EditorApplication.isPlaying) {
				DisableRecompileIfPossible();
			} else {
				RestoreRecompile();
			}
		}

		private static void DisableRecompileIfPossible () {
			var execDisableRecompile = EditorPrefs.GetBool(DISABLE_RECOMPILE_PREF_KEY, true);
			if (!execDisableRecompile)
				return;

			if (Application.platform != RuntimePlatform.WindowsEditor) {
				Debug.LogWarning("Currently, Recompile Disabler only supports Windows Editor");
				return;
			}

			DisableRecompile(true);
		}

		private static void RestoreRecompile () {
			// for safe, it always restores compiler
			var success = DisableRecompile(false);
			if (success)
				AssetDatabase.Refresh();
		}

		/// <returns>true if file has moved</returns>
		private static bool DisableRecompile (bool disable) {
			var disabledPath = Path.GetFullPath(GetMonoLibDirectory() + "/_gmcs.exe");
			var enabledPath = Path.GetFullPath(GetMonoLibDirectory() + "/gmcs.exe");

			var fileHasMoved = false;
			var oldPath = disable ? enabledPath : disabledPath;
			var newPath = disable ? disabledPath : enabledPath;

			try {
				if (File.Exists(oldPath)) {
					File.Move(oldPath, newPath);
					fileHasMoved = true;
				}
			} catch (Exception ex) {
				Debug.LogWarning(ex.Message);
				Debug.LogWarning("Please see this file if you have some problem: " + oldPath);
			}

			return fileHasMoved;
		}

		private static string GetMonoLibDirectory () {
			var procPath = Diag.Process.GetCurrentProcess().MainModule.FileName;
			var editorPath = Path.GetDirectoryName(procPath);
			return Path.GetFullPath(editorPath + "./Data/Mono/lib/mono/2.0");
		}
	}
}
