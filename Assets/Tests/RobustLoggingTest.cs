using System;
using UnityEngine;

namespace Tests
{
    /// <summary>
    /// Test class to verify the robust logging system captures all log types
    /// </summary>
    public class RobustLoggingTest : MonoBehaviour
    {
        void Start()
        {
            TestLoggingSystem();
        }
        
        [ContextMenu("Test Robust Logging")]
        public void TestLoggingSystem()
        {
            Debug.Log("========================================");
            Debug.Log("[RobustLoggingTest] Starting comprehensive test");
            Debug.Log($"Test started at: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            Debug.Log("========================================");
            
            // Test info messages
            Debug.Log("[TEST] Info: Standard information message");
            Debug.Log($"[TEST] Info: Message with timestamp {DateTime.Now.Ticks}");
            
            // Test warnings
            Debug.LogWarning("[TEST] Warning: This is a warning message");
            Debug.LogWarning("[TEST] Warning: Performance might be degraded");
            
            // Test errors
            Debug.LogError("[TEST] Error: Simulated error condition");
            Debug.LogError("[TEST] Error: Critical system failure simulation");
            
            // Test exceptions
            try
            {
                throw new InvalidOperationException("Test exception from RobustLoggingTest");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
            try
            {
                throw new NullReferenceException("Simulated null reference");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
            // Test assertions
            Debug.Assert(1 == 1, "[TEST] This assertion should pass");
            Debug.Assert(false, "[TEST] This assertion should fail and be logged");
            
            // Test multiline messages
            Debug.Log("[TEST] Multiline message:\n  Line 1: First line\n  Line 2: Second line\n  Line 3: Third line");
            
            // Test special characters
            Debug.Log("[TEST] Special chars: !@#$%^&*()_+-=[]{}|;':\",./<>?");
            
            // Test very long message
            string longMessage = "[TEST] Long message: " + new string('X', 500);
            Debug.Log(longMessage);
            
            // Test rapid succession
            for (int i = 1; i <= 5; i++)
            {
                Debug.Log($"[TEST] Rapid log #{i}/5");
            }
            
            // Test with context object
            Debug.Log("[TEST] Message with context object", this);
            
            // Summary
            Debug.Log("========================================");
            Debug.Log("[RobustLoggingTest] Test completed successfully!");
            Debug.Log("All logs should be captured even during compilation errors!");
            Debug.Log("========================================");
        }
    }
}