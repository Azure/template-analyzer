// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Armory.TemplateProcessor.UnitTests
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
