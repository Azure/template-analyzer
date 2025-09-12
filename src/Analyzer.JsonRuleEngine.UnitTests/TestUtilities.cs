// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Expressions;
using Microsoft.Azure.Templates.Analyzer.Types;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    /// <summary>
    /// A set of commonly used utilities in the tests.
    /// </summary>
    public class TestUtilities
    {
        /// <summary>
        /// Creates JSON with 'value' as the value of a key, parses it, then selects that key.
        /// </summary>
        public static JToken ToJToken(object value)
        {
            var jsonValue = JsonConvert.SerializeObject(value);
            var jsonString = $"{{\"Key\": {jsonValue} }}";
            var reader = new JsonTextReader(new StringReader(jsonString))
            {
                DateParseHandling = DateParseHandling.None
            };
            return JObject.Load(reader)["Key"];
        }

        /// <summary>
        /// A mock implementation of an <see cref="Expression"/> for testing internal methods.
        /// </summary>
        internal class MockExpression : Expression
        {
            public Func<IJsonPathResolver, IEnumerable<JsonRuleEvaluation>> EvaluationCallback { get; set; }

            public MockExpression(ExpressionCommonProperties commonProperties)
                : base(commonProperties)
            { }

            /// <summary>
            /// Calls <see cref="Expression.EvaluateInternal(IJsonPathResolver, Func{IJsonPathResolver, IEnumerable{JsonRuleEvaluation}})"/> with <see cref="EvaluationCallback"/>.
            /// </summary>
            public override IEnumerable<JsonRuleEvaluation> Evaluate(IJsonPathResolver jsonScope, ISourceLocationResolver lineNumberResolver)
                => base.EvaluateInternal(jsonScope, EvaluationCallback);
        }

        /// <summary>
        /// Creates a JToken representing an AKS managed cluster resource.
        /// </summary>
        public static JToken CreateAksResource(string location, string kubernetesVersion)
        {
            var resource = new
            {
                type = "Microsoft.ContainerService/managedClusters",
                apiVersion = "2025-06-02-preview",
                name = "test-aks-cluster",
                location = location,
                properties = new
                {
                    kubernetesVersion = kubernetesVersion,
                    linuxProfile = new
                    {
                        adminUsername = "azureuser",
                        ssh = new
                        {
                            publicKeys = new[]
                            {
                                new { keyData = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAABAQ..." }
                            }
                        }
                    }
                }
            };
            
            return JObject.FromObject(resource);
        }
    }
}