// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.RegularExpressions;

namespace Armory.TemplateProcessor.UnitTests
{
    [TestClass]
    public class PlaceholderInputGeneratorTests
    {
        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValue_SetValueToDefaultString()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultString0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_SecureStringParameterMissingDefaultValue_SetValueToDefaultString()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""secureString""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultString0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasMinLength_SetValueToDefaultStringWithMinLength()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""minLength"": 20
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultStringaaaaaa0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasMaxLength_SetValueToDefaultStringWithMaxLength()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""maxLength"": 10
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultSt0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasMinLessThanMaxLength_SetValueToDefaultStringWithMaxLength()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""minLength"": 10,
                        ""maxLength"": 13
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultStrin0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasMinEqualToMaxLength_SetValueToDefaultStringWithMaxLength()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""minLength"": 10,
                        ""maxLength"": 10
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultSt0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        // The Input Generator will not take care of this error. Instead, it will pass some value to the
        // Deployments Template Parsing library which is responsible for throwing the appropriate detailed error.
        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasMinGreaterThanMaxLength_SetValueToDefaultStringWithMaxLength()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""minLength"": 10,
                        ""maxLength"": 6
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defau0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndHasAllowedValues_SetValueToFirstAllowedValue()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""allowedValues"": [
                            ""AllowedValue0"",
                            ""AllowedValue1""
                        ]
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""AllowedValue0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_StringParameterMissingDefaultValueAndEmptyAllowedValues_SetValueToDefaultString()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""string"",
                        ""allowedValues"": [ ]
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": ""defaultString0""
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_IntParameterMissingDefaultValue_SetValueTo1()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""int""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": 1
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_BoolParameterMissingDefaultValue_SetValueToTrue()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""bool""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": true
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_ArrayParameterMissingDefaultValue_SetValueToDefaultArray()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""array""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": [
                            ""item1"",
                            ""item2""
                        ]
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_ObjectParameterMissingDefaultValue_SetValueToDefaultObject()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""object""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": {
                            ""property1"": ""value1""
                        }
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        [TestMethod]
        public void GenerateParameters_SecureObjectParameterMissingDefaultValue_SetValueToDefaultObject()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingDefaultParameter1"": {
                        ""type"": ""secureObject""
                    },
                    ""hasDefaultParameter1"": {
                        ""type"": ""string"",
                        ""defaultValue"": ""defaultValue""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": {                  
                    ""missingDefaultParameter1"": {  
                        ""value"": {
                            ""property1"": ""value1""
                        }
                    }
                }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        // The Input Generator will not take care of this error. Instead, it will pass no value to the
        // Deployments Template Parsing library which is responsible for throwing the appropriate detailed error.
        [TestMethod]
        public void GenerateParameters_InvalidTypeParameter_NoValueSet()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""invalidTypeParameter1"": {
                        ""type"": ""secure""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": { }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        // The Input Generator will not take care of this error. Instead, it will pass no value to the
        // Deployments Template Parsing library which is responsible for throwing the appropriate detailed error.
        [TestMethod]
        public void GenerateParameters_ParameterTypeIsUndefined_NoValueSet()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""notSpecifiedTypeParameter1"": {
                        ""type"": ""notSpecified""
                    }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": { }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        // The Input Generator will not take care of this error. Instead, it will pass no value to the
        // Deployments Template Parsing library which is responsible for throwing the appropriate detailed error.
        [TestMethod]
        public void GenerateParameters_ParameterTypeIsNotSpecified_NoValueSet()
        {
            string armTemplate = @"{
                ""parameters"": {
                    ""missingTypeParameter1"": { }
                },
                ""variables"": {},
                ""resources"": [],
                ""outputs"": {}
            }";

            string expectedParameters = @"{  
                ""parameters"": { }
            }";

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

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

            string generatedParameters = PlaceholderInputGenerator.GenerateParameters(armTemplate);

            Assert.AreEqual(NormalizeString(expectedParameters), NormalizeString(generatedParameters));
        }

        // Only keep non-whitespace characters in the string.
        // This method is used to simplify the assertions for tests
        // by not requiring an exact match of the json formatting.
        private string NormalizeString(string stringToNormalize)
        {
            return Regex.Replace(stringToNormalize, @"\s", "");
        }
    }
}
