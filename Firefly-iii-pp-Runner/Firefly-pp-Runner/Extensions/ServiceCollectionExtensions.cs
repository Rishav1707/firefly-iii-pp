﻿using Firefly_iii_pp_Runner.Services;
using Firefly_iii_pp_Runner.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Polly.Timeout;

namespace Firefly_iii_pp_Runner.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFireflyIIIPPRunnerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<FireflyIIISettings>(configuration.GetSection(nameof(FireflyIIISettings)));
            services.Configure<NodeRedSettings>(configuration.GetSection(nameof(NodeRedSettings)));
            services.Configure<ThunderClientEditorSettings>(configuration.GetSection(nameof(ThunderClientEditorSettings)));

            services.AddSingleton<FireflyIIIService>();
            services.AddSingleton<NodeRedService>();
            services.AddSingleton<JobManager>();
            services.AddSingleton<ThunderClientEditorService>();

            services.AddHttpClient<FireflyIIIService>()
                .AddPolicyHandler(GetRetryPolicy());

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var logger = LoggerFactory.Create(builder =>
            {
                builder.ClearProviders();
                builder.AddConsole();
            }).CreateLogger<Policy>();
            var policy2 = Policy<HttpResponseMessage>
                .Handle<TimeoutRejectedException>()  // firefly-iii sometimes gives socket errors
                .WaitAndRetryAsync<HttpResponseMessage>(5, retryAttempt => TimeSpan.FromSeconds(2), (e, t, i, c) =>
                {
                    logger.LogInformation("Retrying http call (retry attempt: {attempt}) due to error: {error}", i, e.Exception?.Message ?? "null exception");
                });
            var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(2);
            return Policy.WrapAsync(policy2, timeoutPolicy);
        }
    }
}
