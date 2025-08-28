using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing debug settings and logging
    /// </summary>
    public static class DebugService
    {
        #region Constants
        
        private const string PERSPEC_DEBUG_SYMBOL = "PERSPEC_DEBUG";
        
        #endregion
        
        #region Properties
        
        public static bool IsDebugEnabled
        {
            get
            {
                var namedTarget = NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
                string symbols = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
                return HasSymbol(symbols, PERSPEC_DEBUG_SYMBOL);
            }
        }
        
        public static string DebugStatus => IsDebugEnabled 
            ? "Enabled - Debug logs included" 
            : "Disabled - Debug logs stripped";
            
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Enable debug logging
        /// </summary>
        public static void EnableDebugLogging()
        {
            SetDebugEnabled(true);
        }
        
        /// <summary>
        /// Disable debug logging
        /// </summary>
        public static void DisableDebugLogging()
        {
            SetDebugEnabled(false);
        }
        
        /// <summary>
        /// Toggle debug logging
        /// </summary>
        public static void ToggleDebugLogging()
        {
            SetDebugEnabled(!IsDebugEnabled);
        }
        
        /// <summary>
        /// Test all log levels
        /// </summary>
        public static void TestLogLevels()
        {
            Debug.Log("[TEST] Regular log message");
            Debug.LogWarning("[TEST] Warning message");
            Debug.LogError("[TEST] Error message");
            
#if PERSPEC_DEBUG
            PerSpec.PerSpecDebug.Log("[PERSPEC] Debug log (only if enabled)");
            PerSpec.PerSpecDebug.LogTestSetup("Test setup message");
            PerSpec.PerSpecDebug.LogTestComplete("Test complete message");
            PerSpec.PerSpecDebug.LogError("[PERSPEC] Debug error");
#else
            Debug.Log("[PERSPEC] Debug logging is disabled - PerSpecDebug calls are stripped");
#endif
        }
        
        #endregion
        
        #region Private Methods
        
        private static void SetDebugEnabled(bool enabled)
        {
            // Apply to all named build targets
            var namedTargets = new[]
            {
                NamedBuildTarget.Standalone,
                NamedBuildTarget.iOS,
                NamedBuildTarget.Android,
                NamedBuildTarget.WebGL,
                NamedBuildTarget.WindowsStoreApps,
                NamedBuildTarget.tvOS,
                NamedBuildTarget.LinuxHeadlessSimulation,
                NamedBuildTarget.Server
            };
            
            foreach (var target in namedTargets)
            {
                try
                {
                    string currentSymbols = PlayerSettings.GetScriptingDefineSymbols(target);
                    string newSymbols = enabled 
                        ? AddSymbol(currentSymbols, PERSPEC_DEBUG_SYMBOL)
                        : RemoveSymbol(currentSymbols, PERSPEC_DEBUG_SYMBOL);
                    
                    if (currentSymbols != newSymbols)
                    {
                        PlayerSettings.SetScriptingDefineSymbols(target, newSymbols);
                    }
                }
                catch
                {
                    // Some build targets might not be available
                    continue;
                }
            }
            
            // Force recompilation
            AssetDatabase.Refresh();
            UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
            
            Debug.Log($"[PerSpec] Debug logging {(enabled ? "ENABLED" : "DISABLED")}. Recompiling scripts...");
        }
        
        private static bool HasSymbol(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return false;
            
            var symbolList = symbols.Split(';');
            return symbolList.Contains(symbol);
        }
        
        private static string AddSymbol(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return symbol;
            
            if (HasSymbol(symbols, symbol))
                return symbols;
            
            return symbols + ";" + symbol;
        }
        
        private static string RemoveSymbol(string symbols, string symbol)
        {
            if (string.IsNullOrEmpty(symbols))
                return string.Empty;
            
            var symbolList = symbols.Split(';').ToList();
            symbolList.Remove(symbol);
            return string.Join(";", symbolList);
        }
        
        #endregion
    }
}