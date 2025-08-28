using System.Diagnostics;
using UnityEngine;

namespace PerSpec
{
    /// <summary>
    /// Debug logging wrapper for PerSpec framework.
    /// All methods are stripped from builds when PERSPEC_DEBUG is not defined.
    /// Enable/disable via Tools > PerSpec > Debug Settings in Unity Editor.
    /// </summary>
    public static class PerSpecDebug
    {
        #region Log Methods
        
        /// <summary>
        /// Logs a message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Log(object message)
        {
            UnityEngine.Debug.Log(message);
        }
        
        /// <summary>
        /// Logs a message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context)
        {
            UnityEngine.Debug.Log(message, context);
        }
        
        /// <summary>
        /// Logs a formatted message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }
        
        /// <summary>
        /// Logs a formatted message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFormat(Object context, string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(context, format, args);
        }
        
        #endregion
        
        #region LogError Methods
        
        /// <summary>
        /// Logs an error message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogError(object message)
        {
            UnityEngine.Debug.LogError(message);
        }
        
        /// <summary>
        /// Logs an error message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogError(object message, Object context)
        {
            UnityEngine.Debug.LogError(message, context);
        }
        
        /// <summary>
        /// Logs a formatted error message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }
        
        /// <summary>
        /// Logs a formatted error message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogErrorFormat(Object context, string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(context, format, args);
        }
        
        #endregion
        
        #region LogWarning Methods
        
        /// <summary>
        /// Logs a warning message to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning(message);
        }
        
        /// <summary>
        /// Logs a warning message with context to the Unity Console (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }
        
        #endregion
        
        #region Test-Specific Logging
        
        /// <summary>
        /// Logs a test setup message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogTestSetup(string message)
        {
            UnityEngine.Debug.Log($"[TEST-SETUP] {message}");
        }
        
        /// <summary>
        /// Logs a test action message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogTestAction(string message)
        {
            UnityEngine.Debug.Log($"[TEST-ACT] {message}");
        }
        
        /// <summary>
        /// Logs a test assertion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogTestAssert(string message)
        {
            UnityEngine.Debug.Log($"[TEST-ASSERT] {message}");
        }
        
        /// <summary>
        /// Logs a test completion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogTestComplete(string message)
        {
            UnityEngine.Debug.Log($"[TEST-COMPLETE] {message}");
        }
        
        /// <summary>
        /// Logs a test error message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogTestError(string message)
        {
            UnityEngine.Debug.LogError($"[TEST-ERROR] {message}");
        }
        
        #endregion
        
        #region Feature-Specific Logging
        
        /// <summary>
        /// Logs a feature start message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFeatureStart(string feature, string message)
        {
            UnityEngine.Debug.Log($"[{feature.ToUpper()}-START] {message}");
        }
        
        /// <summary>
        /// Logs a feature progress message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFeatureProgress(string feature, string message)
        {
            UnityEngine.Debug.Log($"[{feature.ToUpper()}-PROGRESS] {message}");
        }
        
        /// <summary>
        /// Logs a feature completion message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFeatureComplete(string feature, string message)
        {
            UnityEngine.Debug.Log($"[{feature.ToUpper()}-COMPLETE] {message}");
        }
        
        /// <summary>
        /// Logs a feature error message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void LogFeatureError(string feature, string message)
        {
            UnityEngine.Debug.LogError($"[{feature.ToUpper()}-ERROR] {message}");
        }
        
        #endregion
        
        #region Assertion Methods
        
        /// <summary>
        /// Assert a condition in debug builds (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void Assert(bool condition, string message = null)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogError($"[ASSERT-FAILED] {message ?? "Assertion failed"}");
                UnityEngine.Debug.Break();
            }
        }
        
        /// <summary>
        /// Assert a condition with formatted message (stripped in production)
        /// </summary>
        [Conditional("PERSPEC_DEBUG"), Conditional("UNITY_EDITOR")]
        public static void AssertFormat(bool condition, string format, params object[] args)
        {
            if (!condition)
            {
                UnityEngine.Debug.LogErrorFormat($"[ASSERT-FAILED] {format}", args);
                UnityEngine.Debug.Break();
            }
        }
        
        #endregion
    }
}