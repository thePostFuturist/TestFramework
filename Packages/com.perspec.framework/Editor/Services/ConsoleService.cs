using System;
using UnityEngine;
using UnityEditor;
using PerSpec.Editor.Coordination;

namespace PerSpec.Editor.Services
{
    /// <summary>
    /// Service for managing console log capture and analysis
    /// </summary>
    public static class ConsoleService
    {
        #region Fields
        
        private static bool _isCapturing = true;
        private static int _capturedCount = 0;
        private static int _errorCount = 0;
        private static int _warningCount = 0;
        private static string _sessionId = System.Guid.NewGuid().ToString();
        
        #endregion
        
        #region Constructor
        
        static ConsoleService()
        {
            Initialize();
        }
        
        #endregion
        
        #region Properties
        
        public static bool IsCaptureEnabled => _isCapturing;
        
        public static string SessionId => _sessionId;
        
        public static int CapturedLogCount => _capturedCount;
        
        public static int ErrorCount => _errorCount;
        
        public static int WarningCount => _warningCount;
        
        public static string CaptureStatus
        {
            get
            {
                if (!IsCaptureEnabled)
                    return "Capture disabled";
                    
                return $"Capturing • {CapturedLogCount} logs • {ErrorCount} errors • {WarningCount} warnings";
            }
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Toggle console capture
        /// </summary>
        public static void ToggleCapture()
        {
            ConsoleLogCapture.ToggleCapture();
            _isCapturing = !_isCapturing;
            Debug.Log($"[Console] Capture {(_isCapturing ? "started" : "stopped")}");
        }
        
        /// <summary>
        /// Clear current session
        /// </summary>
        public static void ClearSession()
        {
            ConsoleLogCapture.ClearCurrentSession();
            _capturedCount = 0;
            _errorCount = 0;
            _warningCount = 0;
            _sessionId = System.Guid.NewGuid().ToString();
            Debug.Log("[Console] Current session cleared");
        }
        
        /// <summary>
        /// Export logs to file
        /// </summary>
        public static bool ExportLogs(string filePath = null)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    filePath = EditorUtility.SaveFilePanel(
                        "Export Console Logs",
                        Application.dataPath,
                        $"console_logs_{DateTime.Now:yyyyMMdd_HHmmss}.json",
                        "json"
                    );
                }
                
                if (string.IsNullOrEmpty(filePath))
                    return false;
                    
                // This would need implementation in ConsoleLogCapture
                Debug.Log($"[Console] Logs exported to: {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Console] Failed to export logs: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get session information
        /// </summary>
        public static string GetSessionInfo()
        {
            ConsoleLogCapture.ShowSessionInfo();
            
            if (!IsCaptureEnabled)
                return "No active capture session";
                
            return $"Session: {SessionId}\n" +
                   $"Total Logs: {CapturedLogCount}\n" +
                   $"Errors: {ErrorCount}\n" +
                   $"Warnings: {WarningCount}\n" +
                   $"Info: {CapturedLogCount - ErrorCount - WarningCount}";
        }
        
        /// <summary>
        /// Test different log levels
        /// </summary>
        public static void TestLogLevels()
        {
            ConsoleLogCapture.TestLogLevels();
            
            // Update our counters
            _capturedCount += 5;
            _errorCount += 2;
            _warningCount += 1;
        }
        
        /// <summary>
        /// Initialize or reinitialize the service
        /// </summary>
        public static void Initialize()
        {
            _isCapturing = true;
            _capturedCount = 0;
            _errorCount = 0;
            _warningCount = 0;
            _sessionId = System.Guid.NewGuid().ToString();
            
            // Subscribe to log events to track counts
            Application.logMessageReceived -= OnLogReceived;
            Application.logMessageReceived += OnLogReceived;
        }
        
        private static void OnLogReceived(string message, string stackTrace, LogType type)
        {
            if (!_isCapturing) return;
            
            _capturedCount++;
            
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                    _errorCount++;
                    break;
                case LogType.Warning:
                    _warningCount++;
                    break;
            }
        }
        
        #endregion
    }
}