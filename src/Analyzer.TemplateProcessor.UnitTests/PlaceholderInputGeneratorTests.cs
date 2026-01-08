// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.Azure.Templates.Analyzer.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor.UnitTests
{
    [TestClass]
    public class PlaceholderInputGeneratorTests
    {
        const string DefaultStringParameterValue = @"""defaultString0""";
        const string DefaultStringParameterValueWithMaxLength = @"""defaultSt0""";
        const string DefaultObjectParameterValue = @"{ ""property1"": ""value1"" }";

        [DataTestMethod]
        [DataRow(@"""type"": ""string""", DefaultStringParameterValue, DisplayName = "Type: string, defaultString0 returned")]
        [DataRow(@"""type"": ""secureString""", DefaultStringParameterValue, DisplayName = "Type: secureString, defaultString0 returned")]
        [DataRow(@"""type"": ""string"", ""minLength"": 20", @"""defaultStringaaaaaa0""", DisplayName = "Type: string, MinLength defined, defaultStringaaaaaa0 returned")]
        [DataRow(@"""type"": ""string"", ""maxLength"": 10", DefaultStringParameterValueWithMaxLength, DisplayName = "Type: string, MaxLength defined, defaultSt0 returned")]
        [DataRow(@"""type"": ""string"", ""minLength"": 10, ""maxLength"": 13", @"""defaultStrin0""", DisplayName = "Type: string, MinLength < MaxLength, defaultStrin0 returned")]
        [DataRow(@"""type"": ""string"", ""minLength"": 10, ""maxLength"": 10", DefaultStringParameterValueWithMaxLength, DisplayName = "Type: string, MinLength == MaxLength, defaultSt0 returned")]
        [DataRow(@"""type"": ""string"", ""allowedValues"": [ ""AllowedValue0"", ""AllowedValue1"" ]", @"""AllowedValue0""", DisplayName = "Type: string, AllowedValues: array with values, AllowedValue0 returned")]
        [DataRow(@"""type"": ""string"", ""allowedValues"": [ ]", DefaultStringParameterValue, DisplayName = "Type: string, AllowedValues: empty array, defaultString0 returned")]
        [DataRow(@"""type"": ""int""", "1", DisplayName = "Type: int, 1 returned")]
        [DataRow(@"""type"": ""bool""", "true", DisplayName = "Type: bool, true returned")]
        [DataRow(@"""type"": ""array""", @"[ ""item1"", ""item2"" ]", DisplayName = "Type: array, default array is returned")]
        [DataRow(@"""type"": ""object""", DefaultObjectParameterValue, DisplayName = "Type: object, default object is returned")]
        [DataRow(@"""type"": ""secureObject""", DefaultObjectParameterValue, DisplayName = "Type: secureObject, default object is returned")]
        // The Input Generator will not take care of these errors. Instead, it will pass some value to the
        // Deployments Template Parsing library which is responsible for throwing the appropriate detailed error.
        [DataRow(@"""type"": ""string"", ""minLength"": 14, ""maxLength"": 10", DefaultStringParameterValueWithMaxLength, DisplayName = "Type: string, MinLength > MaxLength, empty parameter object returned")]
        [DataRow(@"""type"": ""secure""", null, DisplayName = "Type: invalid type, empty parameter object returned")]
        [DataRow(@"""type"": ""notSpecified""", null, DisplayName = "Type: notSupported, empty parameter object returned")]
        [DataRow(@"""missingTypeParameter1"": { }", null, DisplayName = "Type: not specified, empty parameter object returned")]
        public void GenerateParameters_SingleParameterMissingDefaultValue_ExpectedValueIsReturned(string parameterMetadata, string expectedParameterValue)
        {
            string armTemplate = GenerateTemplate(parameterMetadata);
            string expectedParameters = expectedParameterValue == null ? @"{""parameters"": { } }" : GenerateExpectedParameters(expectedParameterValue);

            string generatedParameters = PlaceholderInputGenerator.GeneratePlaceholderParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_TwoStringParameterMissingDefaultValue_SetDifferentValuesForEachStringParameter()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter0"": {
                        ""type"": ""string""
                    },
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter0"": {  
                        ""value"": ""defaultString0""
                    },
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultString1""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GeneratePlaceholderParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_ParameterValuesUseAzdVariableSubstitution_ValuesAreGeneratedOrUseDefaultsCorrectly()
        {
            JObject template = JObject.FromObject(new
            {
                parameters = new
                {
                    paramWithDefault = new { type = "string", defaultValue = "myDefaultValue" }, // default provided, won't generate value for missing param
                    paramWithDefaultOverridden = new { type = "string", defaultValue = "shouldBeOverriddenByParamsFile" }, // default provided, but provided parameters should override
                    intParam = new { type = "int" },
                    boolParam = new { type = "bool" },
                    stringParam = new { type = "string" },
                    secureStringParam = new { type = "securestring" },
                    intParamWithAzdDefault = new { type = "int" },
                    boolParamWithAzdDefaultFalse = new { type = "bool" },
                    boolParamWithAzdDefaultTrue= new { type = "bool" },
                    stringParamWithAzdDefault = new { type = "string" },
                    secureStringParamWithAzdDefault = new { type = "securestring" },
                    intParamValueSpecified = new { type = "int" },
                    boolParamValueSpecified = new { type = "bool" },
                    stringParamValueSpecified = new { type = "string" },
                    objectParam = new { type = "object" }, // won't come from env var
                    arrayParam = new { type = "array" } // won't come from env var
                },
                variables = new JObject(),
                resources = new JArray(),
                outputs = new JObject()
            });

            JObject providedParameters = JObject.FromObject(new
            {
                parameters = new
                {
                    paramWithDefaultOverridden = new { value = "valueFromParamsFile" },
                    intParam = new { value = "${MY_INT_VALUE}" },
                    boolParam = new { value = "${MY_BOOL_VALUE}" },
                    stringParam = new { value = "${MY_STRING_VALUE}" },
                    secureStringParam = new { value = "${MY_SECURE_STRING_VALUE}" },
                    intParamWithAzdDefault = new { value = "${MY_INT_VALUE=9}" },
                    boolParamWithAzdDefaultFalse = new { value = "${MY_BOOL_VALUE=false}" },
                    boolParamWithAzdDefaultTrue = new { value = "${MY_BOOL_VALUE=true}" },
                    stringParamWithAzdDefault = new { value = "${MY_STRING_VALUE=defaultAzdStr}" },
                    secureStringParamWithAzdDefault = new { value = "${MY_SECURE_STRING_VALUE=insecureDefaultValue}" },
                    intParamValueSpecified = new { value = 12 },
                    boolParamValueSpecified = new { value = false },
                    stringParamValueSpecified = new { value = "explicitValue" }
                }
            });

            string generatedParameters = PlaceholderInputGenerator.GeneratePlaceholderParameters(template.ToString(), providedParameters.ToString());

            JToken parametersToValidate = JObject.Parse(generatedParameters).InsensitiveToken("parameters");

            Assert.AreEqual("valueFromParamsFile", parametersToValidate.InsensitiveToken("paramWithDefaultOverridden.value").ToString());
            Assert.AreEqual(1, parametersToValidate.InsensitiveToken("intParam.value").Value<int>());
            Assert.IsTrue(parametersToValidate.InsensitiveToken("boolParam.value").Value<bool>());
            Assert.AreEqual("defaultString0", parametersToValidate.InsensitiveToken("stringParam.value").ToString());
            Assert.AreEqual("defaultString1", parametersToValidate.InsensitiveToken("secureStringParam.value").ToString());
            Assert.AreEqual(9, parametersToValidate.InsensitiveToken("intParamWithAzdDefault.value").Value<int>());
            Assert.IsFalse(parametersToValidate.InsensitiveToken("boolParamWithAzdDefaultFalse.value").Value<bool>());
            Assert.IsTrue(parametersToValidate.InsensitiveToken("boolParamWithAzdDefaultTrue.value").Value<bool>());
            Assert.AreEqual("defaultAzdStr", parametersToValidate.InsensitiveToken("stringParamWithAzdDefault.value").ToString());
            Assert.AreEqual("insecureDefaultValue", parametersToValidate.InsensitiveToken("secureStringParamWithAzdDefault.value").ToString());
            Assert.AreEqual(12, parametersToValidate.InsensitiveToken("intParamValueSpecified.value").Value<int>());
            Assert.IsFalse(parametersToValidate.InsensitiveToken("boolParamValueSpecified.value").Value<bool>());
            Assert.AreEqual("explicitValue", parametersToValidate.InsensitiveToken("stringParamValueSpecified.value").ToString());
            Assert.AreEqual("{\"value\":{\"property1\":\"value1\"}}", parametersToValidate.InsensitiveToken("objectParam").ToString(Newtonsoft.Json.Formatting.None));
            Assert.AreEqual("{\"value\":[\"item1\",\"item2\"]}", parametersToValidate.InsensitiveToken("arrayParam").ToString(Newtonsoft.Json.Formatting.None));
        }

        // Only keep non-whitespace characters in the string.
        // This method is used to simplify the assertions for tests
        // by not requiring an exact match of the json formatting.
        private string NormalizeString(string stringToNormalize)
        {
            return Regex.Replace(stringToNormalize, @"\s", "");
        }
    
        private string GenerateTemplate(string parameterMetadata)
        {
            return string.Format(@"{{
                ""parameters"": {{
                    ""missingDefaultParameter1"": {{
                        {0}
                    }},
                    ""hasDefaultParameter1"": {{
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }}
                }},
                ""variables"": {{}},
                ""resources"": [],
                ""outputs"": {{}}
            }}", parameterMetadata);
        }

        private string GenerateExpectedParameters(string parameterValue)
        {
            return string.Format(@"{{
                ""parameters"": {{
                    ""missingDefaultParameter1"": {{
                        ""value"": {0}
                    }}
                }}
            }}", parameterValue);
        }
    }
}
