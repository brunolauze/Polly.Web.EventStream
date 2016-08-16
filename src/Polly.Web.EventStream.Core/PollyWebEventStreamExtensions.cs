// ***********************************************************************
// Assembly         : Polly.Web.EventStream.Core
// Author           : bruno
// Created          : 08-14-2016
//
// Last Modified By : bruno
// Last Modified On : 08-14-2016
// ***********************************************************************
// <copyright file="PollyWebEventStreamExtensions.cs" company="">
//     Copyright (c) . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Polly.Configuration;
using Polly.Metrics;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Polly.Web.EventStream
{
    /// <summary>
    /// Class PollyWebEventStreamExtensions.
    /// </summary>
    public static class PollyWebEventStreamExtensions
    {
        /// <summary>
        /// Uses the metrics.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="requestPath">The request path.</param>
        /// <returns>IApplicationBuilder.</returns>
        public static IApplicationBuilder UseMetrics(this IApplicationBuilder app, IConfigurationRoot configuration, string requestPath = "/polly.stream")
        {
            return app.Use(async (context, next) =>
            {
                var service = (IMetricsService)app.ApplicationServices.GetService(typeof(IMetricsService));
                if (service == null) service = new MetricsService();
                if (context.Request.Path.ToString().Equals(requestPath))
                {
                    var response = context.Response;
                    response.Headers.Add("Content-Type", "text/event-stream");
                    for (var i = 0; true; ++i)
                    {
                        var metricItems = new List<IHealthMetrics>();
                        await Task.Delay(2 * 1000);
                        foreach (var policy in PolicyRegistry.ResolveAll(configuration))
                        {
                            var metrics = service.GetMetrics(policy);
                            metricItems.Add(metrics);
                            var payload = policy.ToJsonMetrics(metrics);
                            await response.WriteAsync($"data: {payload}\r\r");
                            response.Body.Flush();
                            await Task.Delay(2 * 1000);
                        }
                        
                        await response.WriteAsync($"data: {metricItems.ToJsonMetrics()}\r\r");
                        response.Body.Flush();
                        await Task.Delay(2 * 1000);
                    }
                }

                await next.Invoke();
            })
            .UseFileServer(new FileServerOptions
            { 
                RequestPath = "/polly.dashboard",
                FileProvider = new Microsoft.Extensions.FileProviders.EmbeddedFileProvider(typeof(PollyWebEventStreamExtensions).GetTypeInfo().Assembly, "Polly.Web.EventStream.Core")
            });
        }
    }
}
