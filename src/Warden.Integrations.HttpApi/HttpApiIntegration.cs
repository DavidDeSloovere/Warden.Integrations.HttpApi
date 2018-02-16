﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Warden.Integrations.HttpApi
{
    /// <summary>
    /// Integration with the HTTP API for sending information about performed checks.
    /// </summary>
    public class HttpApiIntegration : IIntegration
    {
        private readonly HttpApiIntegrationConfiguration _configuration;

        public HttpApiIntegration(HttpApiIntegrationConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration),
                    "HTTP API Integration configuration has not been provided.");
            }

            _configuration = configuration;
        }

        /// <summary>
        /// Sends a POST request to the base URL of the HTTP API.
        /// </summary>
        /// <param name="data">Request data that will be serialized to the JSON.</param>
        /// <returns></returns>
        public async Task PostAsync(object data)
        {
            await PostAsync(string.Empty, data);
        }

        /// <summary>
        /// Sends a POST request to the specified endpoint in the HTTP API.
        /// </summary>
        /// <param name="endpoint">Endpoint of the HTTP operation (e.g. /iterations).</param>
        /// <param name="data">Request data that will be serialized to the JSON.</param>
        /// <returns></returns>
        public async Task PostAsync(string endpoint, object data)
        {
            var baseUrl = _configuration.Uri.ToString();
            var fullUrl = baseUrl.GetFullUrl(endpoint);

            await _configuration.HttpServiceProvider().PostAsync(fullUrl,
                data.ToJson(_configuration.JsonSerializerSettings), _configuration.Headers,
                _configuration.Timeout, _configuration.FailFast);
        }

        /// <summary>
        /// Sends a POST request to the Warden Panel API endpoint.
        /// </summary>
        /// <param name="iteration">Iteration object that will be serialized to the JSON.</param>
        /// <returns></returns>
        public async Task PostIterationToWardenPanelAsync(IWardenIteration iteration)
        {
            if (iteration == null)
                throw new ArgumentNullException(nameof(iteration), "Warden iteration can not be null.");
            if (string.IsNullOrWhiteSpace(iteration.WardenName))
                throw new ArgumentException("Warden name can not be empty.", nameof(iteration.WardenName));

            var baseUrl = _configuration.Uri.ToString();
            var fixedWardenName = iteration.WardenName.Replace(" ", "%20");
            var endpoint = GetWardenApiIterationEndpoint(_configuration.OrganizationId, fixedWardenName);
            var fullUrl = baseUrl.GetFullUrl(endpoint);
            await _configuration.HttpServiceProvider().PostAsync(fullUrl,
                iteration.ToJson(_configuration.JsonSerializerSettings), _configuration.Headers,
                _configuration.Timeout, _configuration.FailFast);
        }

        private static string GetWardenApiIterationEndpoint(string organizationId, string wardenName)
            => $"organizations/{organizationId}/wardens/{wardenName}/iterations";

        /// <summary>
        /// Sends a POST request to the new Warden Panel API endpoint.
        /// </summary>
        /// <param name="checkResult">Warden check result object that will be serialized to the JSON.</param>
        /// <returns></returns>
        public async Task PostCheckResultToWardenPanelAsync(IWardenCheckResult checkResult)
        {
            if (checkResult == null)
                throw new ArgumentNullException(nameof(checkResult), "Warden check result can not be null.");

            if (string.IsNullOrWhiteSpace(checkResult.WatcherCheckResult.WatcherName))
            {
                throw new ArgumentException("Watcher name can not be empty.",
                    nameof(checkResult.WatcherCheckResult.WatcherName));
            }

            var baseUrl = _configuration.Uri.ToString();
            var endpoint = GetWardenApiChecksEndpoint(_configuration.OrganizationId, _configuration.WardenId);
            var fullUrl = baseUrl.GetFullUrl(endpoint);
            var data = new {check = checkResult};
            await _configuration.HttpServiceProvider().PostAsync(fullUrl,
                data.ToJson(_configuration.JsonSerializerSettings),
                _configuration.Headers, _configuration.Timeout, _configuration.FailFast);
        }

        private static string GetWardenApiChecksEndpoint(string organizationId, string wardenId)
            => $"organizations/{organizationId}/wardens/{wardenId}/checks";

        /// <summary>
        /// Factory method for creating a new instance of HttpApiIntegration.
        /// </summary>
        /// <param name="url">URL of the HTTP API.</param>
        /// <param name="apiKey">Optional API key of the HTTP API passed inside the custom "X-Api-Key" header.</param>
        /// <param name="organizationId">Optional id of the organization that should be used in the Warden Web Panel.</param>
        /// <param name="wardenId">Optional id of the warden that should be used in the new Warden Web Panel.</param>
        /// <param name="headers">Optional request headers.</param>
        /// <param name="configurator">Lambda expression for configuring the HttpApiIntegration integration.</param>
        /// <returns>Instance of HttpApiIntegration.</returns>
        public static HttpApiIntegration Create(string url, string apiKey = null,
            string organizationId = null, string wardenId = null, IDictionary<string, string> headers = null,
            Action<HttpApiIntegrationConfiguration.Builder> configurator = null)
        {
            var config = new HttpApiIntegrationConfiguration.Builder(url, apiKey, organizationId, wardenId, headers);
            configurator?.Invoke(config);

            return Create(config.Build());
        }

        /// <summary>
        /// Factory method for creating a new instance of HttpApiIntegration.
        /// </summary>
        /// <param name="configuration">Configuration of HTTP API integration.</param>
        /// <returns>Instance of HttpApiIntegration.</returns>
        public static HttpApiIntegration Create(HttpApiIntegrationConfiguration configuration)
            => new HttpApiIntegration(configuration);
    }
}