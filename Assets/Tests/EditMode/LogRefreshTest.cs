using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    [TestFixture]
    public class LogRefreshTest
    {
        [Test]
        public void Should_GenerateLogsForRefreshTest()
        {
            var timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
            
            Debug.Log($"[REFRESH-TEST] {timestamp} - Log message 1");
            Debug.LogWarning($"[REFRESH-TEST] {timestamp} - Warning message");
            Debug.Log($"[REFRESH-TEST] {timestamp} - Log message 2");
            
            // Generate multiple logs to test refresh
            for (int i = 0; i < 5; i++)
            {
                Debug.Log($"[REFRESH-TEST] {timestamp} - Burst log {i}");
            }
            
            Debug.Log($"[REFRESH-TEST] {timestamp} - Test completed");
            
            Assert.IsTrue(true, "Logs generated for refresh testing");
        }
    }
}