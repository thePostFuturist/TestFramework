using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace TestFramework.Unity.Helpers
{
    /// <summary>
    /// Helper class to run UniTasks in tests
    /// Provides centralized async operation management
    /// </summary>
    public class UniTaskRunner : MonoBehaviour
    {
        #region Fields
        
        private CancellationTokenSource applicationCancellationTokenSource;
        private static UniTaskRunner instance;
        
        #endregion
        
        #region Properties
        
        public static UniTaskRunner Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("UniTaskRunner");
                    instance = go.AddComponent<UniTaskRunner>();
                    instance.Initialize();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        /// <summary>
        /// Gets the cancellation token for the application lifetime
        /// </summary>
        public CancellationToken ApplicationToken => applicationCancellationTokenSource?.Token ?? CancellationToken.None;
        
        #endregion
        
        #region Initialization
        
        private void Initialize()
        {
            applicationCancellationTokenSource = new CancellationTokenSource();
        }
        
        #endregion
        
        #region UniTask Methods
        
        /// <summary>
        /// Runs a UniTask async operation
        /// </summary>
        /// <param name="task">The UniTask to run</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async UniTaskVoid RunAsync(Func<UniTask> task, CancellationToken cancellationToken = default)
        {
            try
            {
                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                    applicationCancellationTokenSource.Token,
                    cancellationToken
                ).Token;
                
                await task().AttachExternalCancellation(linkedToken);
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[UniTaskRunner] Task was cancelled");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UniTaskRunner] Task failed: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Runs a UniTask and returns the result
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="task">The UniTask to run</param>
        /// <param name="cancellationToken">Optional cancellation token</param>
        public async UniTask<T> RunWithResultAsync<T>(Func<UniTask<T>> task, CancellationToken cancellationToken = default)
        {
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                applicationCancellationTokenSource.Token,
                cancellationToken
            ).Token;
            
            return await task().AttachExternalCancellation(linkedToken);
        }
        
        /// <summary>
        /// Runs multiple UniTasks in parallel
        /// </summary>
        public async UniTask RunParallelAsync(params Func<UniTask>[] tasks)
        {
            var uniTasks = new UniTask[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                uniTasks[i] = tasks[i]();
            }
            await UniTask.WhenAll(uniTasks);
        }
        
        /// <summary>
        /// Runs multiple UniTasks and returns when any completes
        /// </summary>
        public async UniTask<int> RunAnyAsync(params Func<UniTask>[] tasks)
        {
            var uniTasks = new UniTask[tasks.Length];
            for (int i = 0; i < tasks.Length; i++)
            {
                uniTasks[i] = tasks[i]();
            }
            
            // WhenAny returns the index of the first completed task
            var completedIndex = await UniTask.WhenAny(uniTasks);
            return completedIndex;
        }
        
        /// <summary>
        /// Runs a task with retry logic
        /// </summary>
        public async UniTask<T> RunWithRetryAsync<T>(
            Func<UniTask<T>> task, 
            int maxRetries = 3,
            int delayMs = 1000,
            CancellationToken cancellationToken = default)
        {
            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(
                applicationCancellationTokenSource.Token,
                cancellationToken
            ).Token;
            
            Exception lastException = null;
            
            for (int i = 0; i <= maxRetries; i++)
            {
                try
                {
                    return await task().AttachExternalCancellation(linkedToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    if (i < maxRetries)
                    {
                        Debug.LogWarning($"[UniTaskRunner] Retry {i + 1}/{maxRetries}: {ex.Message}");
                        await UniTask.Delay(delayMs, cancellationToken: linkedToken);
                    }
                }
            }
            
            throw lastException;
        }
        
        /// <summary>
        /// Cancels all running UniTasks
        /// </summary>
        public void CancelAll()
        {
            applicationCancellationTokenSource?.Cancel();
            applicationCancellationTokenSource?.Dispose();
            applicationCancellationTokenSource = new CancellationTokenSource();
        }
        
        #endregion
        
        #region Lifecycle
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                applicationCancellationTokenSource?.Cancel();
                applicationCancellationTokenSource?.Dispose();
                instance = null;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                Debug.Log("[UniTaskRunner] Application paused");
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                Debug.Log("[UniTaskRunner] Application lost focus");
            }
        }
        
        #endregion
    }
}