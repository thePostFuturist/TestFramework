using System;
using UnityEngine;

namespace Tests
{
    public class LoggingSystemTest : MonoBehaviour
    {
        void Start()
        {
            Debug.Log("[LoggingSystemTest] Starting comprehensive logging test...");
            
            // Test various log levels
            Debug.Log("INFO: This is a standard information message");
            Debug.LogWarning("WARNING: This is a warning about something");
            Debug.LogError("ERROR: This is an error that occurred");
            
            // Test with timestamps
            Debug.Log($"[{DateTime.Now:HH:mm:ss.fff}] Timestamped log entry");
            
            // Test with complex objects
            var testData = new { Name = "TestObject", Value = 42, Status = "Active" };
            Debug.Log($"Complex object log: {testData}");
            
            // Test multiline logs
            Debug.Log("This is a multiline log:\n  Line 1\n  Line 2\n  Line 3");
            
            // Test exception logging
            try
            {
                throw new InvalidOperationException("Test exception for logging system");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
            // Test assertion
            Debug.Assert(false, "Test assertion failure - this should be logged");
            
            // Generate rapid succession logs
            for (int i = 0; i < 10; i++)
            {
                Debug.Log($"Rapid log #{i} at {DateTime.Now.Ticks}");
            }
            
            Debug.Log("[LoggingSystemTest] Test complete!");
        }
    }
}