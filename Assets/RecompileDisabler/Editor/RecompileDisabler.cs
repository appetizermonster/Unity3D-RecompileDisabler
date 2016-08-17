using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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

			DisableRecompile(true);
			WatchCompiling();

			compilerDisabled_ = true;
		}

		private static bool compilerDisabled_ = false;
		private static bool reimportScriptsAfterPlay_ = false;

		private static void WatchCompiling () {
			Application.logMessageReceived -= WatchCompiling_OnLogMessage;
			Application.logMessageReceived += WatchCompiling_OnLogMessage;
		}

		private static void WatchCompiling_OnLogMessage (string condition, string stackTrace, LogType type) {
			if (condition.StartsWith("Could not start compilationApplicationException"))
				reimportScriptsAfterPlay_ = true;
		}

		private static void RestoreRecompile () {
			// for safe, it always restores compiler
			var success = DisableRecompile(false);
			if (success && reimportScriptsAfterPlay_)
				ReimportScripts();
		}

		private static void ReimportScripts () {
			var scripts = AssetDatabase.FindAssets("t:MonoScript");
			if (scripts == null || scripts.Length == 0) {
				Debug.LogWarning("RecompileDisabler: Can't find any script");
				return;
			}

			EditorApplication.delayCall += () => {
				Debug.Log("RecompileDisabler: Try to reimport scripts");

				var scriptPath = AssetDatabase.GUIDToAssetPath(scripts[0]);
				AssetDatabase.ImportAsset(scriptPath);
			};
		}

		/// <summary>
		/// Execute any action that uses mono compiler internally, temporally
		/// </summary>
		public static void ExecuteActionWithCompiler (Action action) {
			if (action == null)
				return;

			if (!compilerDisabled_) {
				action();
				return;
			}

			DisableRecompile(false);
			action();
			DisableRecompile(true);
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
				Debug.LogWarning("RecompileDisabler: Please see this file if you have some problem: " + oldPath);
			}

			return fileHasMoved;
		}

		private static string monoLibDirectory_ = null;

		private static string GetMonoLibDirectory () {
			if (monoLibDirectory_ != null)
				return monoLibDirectory_;

			var unityEngineAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault((x) => {
				return (x != null) && x.FullName.Contains("UnityEngine,");
			});
			if (unityEngineAssembly == null)
				throw new FileNotFoundException("RecompileDisabler: Can't find a UnityEngine assembly");

			var unityEngineAssemblyDir = Path.GetDirectoryName(unityEngineAssembly.Location);
			monoLibDirectory_ = Path.GetFullPath(unityEngineAssemblyDir + "/../Mono/lib/mono/2.0");

			return monoLibDirectory_;
		}
	}
}
