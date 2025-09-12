// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators
{
    /// <summary>
    /// Default implementation that fetches stable AKS versions from the Azure API.
    /// </summary>
    internal sealed class DefaultStableAksVersionProvider : IStableAksVersionProvider
    {
        private static readonly Lazy<DefaultStableAksVersionProvider> _instance =
            new Lazy<DefaultStableAksVersionProvider>(() => new DefaultStableAksVersionProvider());

        private readonly object _lock = new object();
        private volatile Dictionary<string, HashSet<string>> _cache;

        /// <summary>
        /// Gets the singleton instance of the provider.
        /// </summary>
        public static DefaultStableAksVersionProvider Instance => _instance.Value;

        private DefaultStableAksVersionProvider() { }

        /// <inheritdoc/>
        public bool TryGetStableVersions(string normalizedLocation, out ISet<string> versions)
        {
            if (string.IsNullOrEmpty(normalizedLocation))
            {
                versions = null;
                return false;
            }

            EnsureInitialized();
            
            if (_cache != null && _cache.TryGetValue(normalizedLocation, out var set))
            {
                versions = set;
                return true;
            }
            
            versions = null;
            return false;
        }

        /// <summary>
        /// Ensures the cache is initialized with stable version data.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_cache != null) return;

            lock (_lock)
            {
                if (_cache != null) return;
                _cache = FetchStableVersions();
            }
        }

        /// <summary>
        /// Fetches stable AKS versions from the Azure API.
        /// </summary>
        /// <returns>A dictionary mapping normalized region names to stable version sets.</returns>
        private Dictionary<string, HashSet<string>> FetchStableVersions()
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");

                var response = httpClient.GetAsync("https://releases.aks.azure.com/webpage/parsed_data.json")
                    .GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();

                var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                var token = JToken.Parse(jsonString);

                var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

                var regionalStatuses = token.SelectToken(
                    "Sections.KubernetesSupportedVersions.Components.KubernetesVersions.RegionalStatuses") as JObject;

                if (regionalStatuses != null)
                {
                    foreach (var continent in regionalStatuses.Properties())
                    {
                        var regions = continent.Value as JArray;
                        if (regions == null) continue;

                        foreach (var region in regions.Children<JObject>())
                        {
                            var regionName = region.Value<string>("RegionName");
                            if (string.IsNullOrEmpty(regionName)) continue;

                            var normalizedRegionName = NormalizeRegionName(regionName);
                            var html = region.SelectToken("Current.Version")?.ToString() ?? "";

                            var versions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            foreach (Match match in Regex.Matches(html, "<a[^>]*>(.*?)</a>",
                                RegexOptions.IgnoreCase | RegexOptions.Singleline))
                            {
                                var version = System.Net.WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
                                if (!string.IsNullOrEmpty(version))
                                {
                                    var cleanVersion = Regex.Replace(version, @"\s*\(LTS\)\s*$", "",
                                        RegexOptions.IgnoreCase);
                                    versions.Add(cleanVersion);
                                }
                            }

                            result[normalizedRegionName] = versions;
                        }
                    }
                }

                return result;
            }
            catch
            {
                // On failure, return empty dictionary
                return new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Normalizes a region name by converting to lowercase and removing spaces.
        /// </summary>
        /// <param name="regionName">The region name to normalize.</param>
        /// <returns>The normalized region name.</returns>
        private static string NormalizeRegionName(string regionName)
        {
            return regionName.ToLowerInvariant().Replace(" ", "");
        }
    }
}