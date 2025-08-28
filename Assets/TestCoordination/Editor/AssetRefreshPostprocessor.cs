using System;
using UnityEngine;
using UnityEditor;

namespace TestCoordination
{
    /// <summary>
    /// Detects when asset refresh operations complete using AssetPostprocessor callbacks
    /// </summary>
    public class AssetRefreshPostprocessor : AssetPostprocessor
    {
        private static int _currentRequestId = -1;
        private static bool _isProcessingRefresh = false;
        
        /// <summary>
        /// Set the current request ID before starting a refresh
        /// </summary>
        public static void SetCurrentRequestId(int requestId)
        {
            _currentRequestId = requestId;
            _isProcessingRefresh = true;
            Debug.Log($"[AssetRefreshPostprocessor] Tracking refresh request #{requestId}");
        }
        
        /// <summary>
        /// Clear the current request ID after completion
        /// </summary>
        public static void ClearCurrentRequestId()
        {
            _currentRequestId = -1;
            _isProcessingRefresh = false;
        }
        
        /// <summary>
        /// Called when all assets have finished importing
        /// </summary>
        static void OnPostprocessAllAssets(
            string[] importedAssets, 
            string[] deletedAssets, 
            string[] movedAssets, 
            string[] movedFromAssetPaths)
        {
            // Only process if we're tracking a refresh request
            if (!_isProcessingRefresh || _currentRequestId < 0)
            {
                return;
            }
            
            // Log what was processed
            Debug.Log($"[AssetRefreshPostprocessor] OnPostprocessAllAssets called for request #{_currentRequestId}");
            
            if (importedAssets.Length > 0)
            {
                Debug.Log($"  Imported: {importedAssets.Length} asset(s)");
                if (importedAssets.Length <= 10)
                {
                    foreach (var asset in importedAssets)
                    {
                        Debug.Log($"    - {asset}");
                    }
                }
            }
            
            if (deletedAssets.Length > 0)
            {
                Debug.Log($"  Deleted: {deletedAssets.Length} asset(s)");
            }
            
            if (movedAssets.Length > 0)
            {
                Debug.Log($"  Moved: {movedAssets.Length} asset(s)");
            }
            
            // Always notify completion when this callback fires during a tracked refresh
            if (importedAssets.Length == 0 && deletedAssets.Length == 0 && movedAssets.Length == 0)
            {
                Debug.Log("[AssetRefreshPostprocessor] No assets changed during refresh");
            }
            else
            {
                Debug.Log("[AssetRefreshPostprocessor] Asset processing completed");
            }
            
            // Notify the coordinator that refresh is complete
            AssetRefreshCoordinator.OnRefreshComplete();
        }
        
        /// <summary>
        /// Called before an asset is imported
        /// </summary>
        void OnPreprocessAsset()
        {
            if (_isProcessingRefresh && _currentRequestId >= 0)
            {
                // Could log individual asset processing if needed
                // Debug.Log($"[AssetRefreshPostprocessor] Processing: {assetPath}");
            }
        }
    }
}