// ***********************************************************************
// Assembly         : Polly.Web.EventStream.Core
// Author           : bruno
// Created          : 08-14-2016
//
// Last Modified By : bruno
// Last Modified On : 08-14-2016
// ***********************************************************************
// <copyright file="IHealthMetricsExtensions.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Polly.Web.EventStream
{
    /// <summary>
    /// Class IHealthMetricsExtensions.
    /// </summary>
    public static class IHealthMetricsExtensions
    {
        /// <summary>
        /// The epoch
        /// </summary>
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        
        delegate void GetMinThreadsNative(out int workerThreads, out int completionPortThreads);
        delegate void GetMaxThreadsNative(out int workerThreads, out int completionPortThreads);

        private static readonly GetMinThreadsNative _getMinThreadsNative;
        private static readonly GetMaxThreadsNative _getMaxThreadsNative;

        static IHealthMetricsExtensions()
        {
#if !DOTNET
            var getMinThreadsNativeMethod = typeof(ThreadPool).GetMethod("GetMinThreadsNative", BindingFlags.Static | BindingFlags.NonPublic);
            _getMinThreadsNative = (GetMinThreadsNative)getMinThreadsNativeMethod.CreateDelegate(typeof(GetMinThreadsNative));
            var getMaxThreadsNativeMethod = typeof(ThreadPool).GetMethod("GetMaxThreadsNative", BindingFlags.Static | BindingFlags.NonPublic);
            _getMaxThreadsNative = (GetMaxThreadsNative)getMaxThreadsNativeMethod.CreateDelegate(typeof(GetMaxThreadsNative));
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        public static string Type { get { return "HystrixThreadPool"; } }

        public static int CurrentPoolSize
        {
            get
            {
#if !DOTNET
                int workerThreads;
                int completionPortThreads;
                _getMinThreadsNative(out workerThreads, out completionPortThreads);
                return workerThreads;
#else
                return 0;
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentActiveCount
        {
            get
            {
                int count = 0;
#if !DOTNET
                foreach (ProcessThread thread in Process.GetCurrentProcess().Threads)
                {
                    if (thread.ThreadState == System.Diagnostics.ThreadState.Running)
                    {
                        count++;
                    }
                }
#endif
                return count;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentLargestPoolSize
        {
            get { return RollingMaxActiveThreads; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentCorePoolSize
        {
            get { return CurrentPoolSize; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentQueueSize
        {
            get { return 0; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentTaskCount
        {
            get { return 0; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentMaximumPoolSize
        {
            get { return CurrentLargestPoolSize; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int RollingMaxActiveThreads
        {
            get
            {
#if DOTNET
                int workerThreads;
                int completionPortThreads;
                _getMaxThreadsNative(out workerThreads, out completionPortThreads);
                return workerThreads;
#else
                return 0;
#endif
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static string Name
        {
            get { return "dotnet"; }
        }

        /// <summary>
        /// 
        /// </summary>
        public static int QueueSizeRejectionThreshold { get { return 0; } }

        /// <summary>
        /// 
        /// </summary>
        public static int ReportingHosts { get { return 1; } }
        


        public static string ToJsonMetrics(this IEnumerable<IHealthMetrics> metrics)
        {
            var sum = metrics.Sum(x => x.RollingCountTotal);
            var avg = metrics.Average(x => x.RollingStatisticalWindowInMilliseconds);
            var currentCompletedTaskCount = metrics.Sum(x => x.CumulativeCountTotal);
            return $"{{ \"currentPoolSize\": {CurrentPoolSize}, \"rollingMaxActiveThreads\": {RollingMaxActiveThreads}, \"currentActiveCount\": {CurrentActiveCount}, \"currentCompletedTaskCount\": {currentCompletedTaskCount},\"propertyValue_queueSizeRejectionThreshold\": {QueueSizeRejectionThreshold},\"type\": \"{Type}\", \"reportingHosts\": 1, \"propertyValue_metricsRollingStatisticalWindowInMilliseconds\": {avg}, \"name\": \"{Name}\", \"currentLargestPoolSize\": {CurrentLargestPoolSize}, \"currentCorePoolSize\": {CurrentCorePoolSize}, \"currentQueueSize\": {CurrentQueueSize},\"currentTaskCount\": {CurrentTaskCount},\"rollingCountThreadsExecuted\": {sum},\"currentMaximumPoolSize\": {CurrentMaximumPoolSize} }}";
        }


        /// <summary>
        /// To the json metrics.
        /// </summary>
        /// <param name="policy">The policy.</param>
        /// <param name="metrics">The metrics.</param>
        /// <returns>System.String.</returns>
        public static string ToJsonMetrics(this Policy policy, IHealthMetrics metrics)
        {
            var latency = policy.GetLatencyMetrics();
            JObject data = new JObject(
                new JProperty("type", "HystrixCommand"),
                new JProperty("name", metrics.PolicyKey),
                new JProperty("group", "dotnet"), //TODO:
                new JProperty("currentTime", CurrentTime),
                new JProperty("isCircuitBreakerOpen", metrics.CircuitState != CircuitBreaker.CircuitState.Closed),
                new JProperty("errorPercentage", metrics.CumulativeErrorsPercentage), // health counts
                new JProperty("errorCount", metrics.CumulativeCountErrors),
                new JProperty("requestCount", metrics.CumulativeCountTotal),
                new JProperty("rollingCountCollapsedRequests", metrics.RollingCountErrors), // rolling counters
                new JProperty("rollingCountExceptionsThrown", metrics.RollingCountExceptionsThrown),
                new JProperty("rollingCountFailure", metrics.RollingCountErrors),
                new JProperty("rollingCountFallbackFailure", metrics.RollingCountFallbackFailure),
                new JProperty("rollingCountFallbackRejection", 0),
                new JProperty("rollingCountFallbackSuccess", metrics.RollingCountFallbackSuccess),
                new JProperty("rollingCountResponsesFromCache", metrics.RollingCountResponsesFromCache),
                new JProperty("rollingCountSemaphoreRejected", metrics.RollingCountSemaphoreRejected),
                new JProperty("rollingCountShortCircuited", metrics.RollingCountShortCircuited),
                new JProperty("rollingCountSuccess", metrics.RollingCountSuccess),
                new JProperty("rollingCountThreadPoolRejected", 0),
                new JProperty("rollingCountTimeout", metrics.RollingCountTimeout),
                new JProperty("currentConcurrentExecutionCount", metrics.CurrentConcurrentExecutionCount),
                new JProperty("latencyExecute_mean", latency.GetMean()), // latency percentiles
                new JProperty(
                    "latencyExecute",
                    new JObject(
                        new JProperty("0", latency.GetPercentile(0)),
                        new JProperty("25", latency.GetPercentile(25)),
                        new JProperty("50", latency.GetPercentile(50)),
                        new JProperty("75", latency.GetPercentile(75)),
                        new JProperty("90", latency.GetPercentile(90)),
                        new JProperty("95", latency.GetPercentile(95)),
                        new JProperty("99", latency.GetPercentile(99)),
                        new JProperty("99.5", latency.GetPercentile(99.5)),
                        new JProperty("100", latency.GetPercentile(100)))),
                new JProperty("latencyTotal_mean", latency.GetMean()),
                new JProperty(
                    "latencyTotal",
                    new JObject(
                        new JProperty("0", latency.GetPercentile(0)),
                        new JProperty("25", latency.GetPercentile(25)),
                        new JProperty("50", latency.GetPercentile(50)),
                        new JProperty("75", latency.GetPercentile(75)),
                        new JProperty("90", latency.GetPercentile(90)),
                        new JProperty("95", latency.GetPercentile(95)),
                        new JProperty("99", latency.GetPercentile(99)),
                        new JProperty("99.5", latency.GetPercentile(99.5)),
                        new JProperty("100", latency.GetPercentile(100)))),
                new JProperty("propertyValue_circuitBreakerRequestVolumeThreshold", 0 /* TODO: */), // property values for reporting what is actually seen by the command rather than what was set somewhere 
                new JProperty("propertyValue_circuitBreakerSleepWindowInMilliseconds", 0 /* TODO: */),
                new JProperty("propertyValue_circuitBreakerErrorThresholdPercentage", 0 /* TODO: */),
                new JProperty("propertyValue_circuitBreakerForceOpen", false),
                new JProperty("propertyValue_circuitBreakerForceClosed", false),
                new JProperty("propertyValue_circuitBreakerEnabled", policy.HasCircuitBreaker()),
                new JProperty("propertyValue_executionIsolationStrategy", "SEMAPHORE"),
                new JProperty("propertyValue_executionIsolationThreadTimeoutInMilliseconds", policy.HasTimeoutPolicy() ? 4000 : 0),
                new JProperty("propertyValue_executionIsolationThreadInterruptOnTimeout", true),
                new JProperty("propertyValue_executionIsolationThreadPoolKeyOverride", null),
                new JProperty("propertyValue_executionIsolationSemaphoreMaxConcurrentRequests", 0),
                new JProperty("propertyValue_fallbackIsolationSemaphoreMaxConcurrentRequests", 0),
                new JProperty("propertyValue_metricsRollingStatisticalWindowInMilliseconds", metrics.RollingStatisticalWindowInMilliseconds),
                new JProperty("propertyValue_requestCacheEnabled", policy.HasCachePolicy()),
                new JProperty("propertyValue_requestLogEnabled", true),
                new JProperty("reportingHosts", 1));

            return data.ToString(Formatting.None);
        }

        /// <summary>
        /// Gets the current time in the format of JavaScript, which is the elapsed
        /// time since 1970.01.01 00:00:00 in milliseconds.
        /// </summary>
        /// <value>The current time.</value>
        private static long CurrentTime
        {
            get
            {
                return (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;
            }
        }



    }
}
