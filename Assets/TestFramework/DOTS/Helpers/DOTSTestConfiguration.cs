using System;
using UnityEngine;

namespace TestFramework.DOTS.Helpers
{
    /// <summary>
    /// Configuration data structure for DOTS test setups
    /// </summary>
    [Serializable]
    public class DOTSTestConfiguration
    {
        public string Name = "TestSetup";
        public bool EnableDiagnostics = true;
        public bool CollectMetrics = true;
        public bool EnableDebugUI = false;
        
        // Generic test configuration fields
        public int TestEntityCount = 100;
        public float TestUpdateInterval = 0.016f; // 60 FPS
        public bool UseJobSystem = true;
        public bool EnableBurstCompilation = true;
        
        /// <summary>
        /// Creates a default test configuration
        /// </summary>
        public static DOTSTestConfiguration CreateDefault()
        {
            return new DOTSTestConfiguration
            {
                Name = "DefaultTest",
                EnableDiagnostics = true,
                CollectMetrics = false
            };
        }
        
        /// <summary>
        /// Creates a configuration for performance testing
        /// </summary>
        public static DOTSTestConfiguration CreatePerformanceTest()
        {
            return new DOTSTestConfiguration
            {
                Name = "PerformanceTest",
                TestEntityCount = 1000,
                TestUpdateInterval = 0.016f,
                UseJobSystem = true,
                EnableBurstCompilation = true,
                CollectMetrics = true,
                EnableDiagnostics = false
            };
        }
        
        /// <summary>
        /// Creates a configuration for stress testing
        /// </summary>
        public static DOTSTestConfiguration CreateStressTest()
        {
            return new DOTSTestConfiguration
            {
                Name = "StressTest",
                TestEntityCount = 10000,
                TestUpdateInterval = 0.008f, // 120 FPS target
                UseJobSystem = true,
                EnableBurstCompilation = true,
                CollectMetrics = true,
                EnableDiagnostics = true
            };
        }
    }
    
    /// <summary>
    /// Example component data structure for testing
    /// Replace with your specific component types
    /// </summary>
    [Serializable]
    public struct TestComponentData
    {
        public float Value;
        public int Count;
        public bool IsActive;
    }
    
    /// <summary>
    /// Example quality metrics configuration
    /// Replace with your specific metrics
    /// </summary>
    [Serializable]
    public class QualityMetricsConfig
    {
        // Performance metrics
        public bool MeasureFrameTime = true;
        public bool MeasureMemoryUsage = true;
        public bool TrackEntityCount = true;
        public bool MeasureJobExecutionTime = true;
        
        // Thresholds
        public float MaxFrameTimeMs = 16.67f; // 60 FPS threshold
        public long MaxMemoryUsageMB = 100;
        public float MaxJobExecutionTimeMs = 10f;
        
        // Update settings
        public float UpdateInterval = 1.0f; // seconds
        public bool LogToFile = true;
        public bool ShowInUI = false;
        
        public static QualityMetricsConfig CreateDefault()
        {
            return new QualityMetricsConfig();
        }
        
        public static QualityMetricsConfig CreatePerformanceMonitoring()
        {
            return new QualityMetricsConfig
            {
                MeasureFrameTime = true,
                MeasureMemoryUsage = true,
                TrackEntityCount = true,
                MeasureJobExecutionTime = true,
                UpdateInterval = 0.5f,
                LogToFile = true
            };
        }
        
        public static QualityMetricsConfig CreateMinimal()
        {
            return new QualityMetricsConfig
            {
                MeasureFrameTime = true,
                MeasureMemoryUsage = false,
                TrackEntityCount = false,
                MeasureJobExecutionTime = false,
                UpdateInterval = 5.0f,
                LogToFile = false
            };
        }
    }
    
    /// <summary>
    /// Template for test state tracking
    /// </summary>
    public enum TestState : byte
    {
        NotStarted = 0,
        Initializing = 1,
        Running = 2,
        Paused = 3,
        Completed = 4,
        Failed = 5
    }
    
    /// <summary>
    /// Template for test priority levels
    /// </summary>
    public enum TestPriority : byte
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
}