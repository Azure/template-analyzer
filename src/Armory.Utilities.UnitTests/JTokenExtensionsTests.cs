// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;

namespace Armory.Utilities.UnitTests
{
    [TestClass]
    public class JTokenExtensionsTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsError_ArgumentNullExceptionThrown()
        {
            JObject jsonObject = null;

            jsonObject.InsensitiveToken("someProperty", InsensitivePathNotFoundBehavior.Error);
        }

        [TestMethod]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsLastValid_NullIsReturned()
        {
            JObject jsonObject = null;

            JToken valueAtPath = jsonObject.InsensitiveToken("someProperty", InsensitivePathNotFoundBehavior.LastValid);
            
            Assert.IsNull(valueAtPath);
        }

        [TestMethod]
        public void InsensitiveToken_JsonObjectIsNullAndInsensitivePathNotFoundBehaviorIsNull_NullIsReturned()
        {
            JObject jsonObject = null;

            JToken valueAtPath = jsonObject.InsensitiveToken("someProperty", InsensitivePathNotFoundBehavior.Null);

            Assert.IsNull(valueAtPath);
        }

        [TestMethod]
        public void InsensitiveToken_JsonPathIsNullAndInsensitivePathNotFoundBehaviorIsNull_NullIsReturned()
        {
            string rawJson = @"{ ""someProperty"": ""expectedValue"" }";
            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken(null, InsensitivePathNotFoundBehavior.Null);

            Assert.IsNull(valueAtPath);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void InsensitiveToken_JsonPathIsNullAndInsensitivePathNotFoundBehaviorIsError_ArgumentExceptionThrown()
        {
            string rawJson = @"{ ""someProperty"": ""expectedValue"" }";
            JObject parsedJson = JObject.Parse(rawJson);

            parsedJson.InsensitiveToken(null, InsensitivePathNotFoundBehavior.Error);
        }

        [TestMethod]
        public void InsensitiveToken_JsonPathHasOneLevel_TheJTokenWithStringValueExpectedValueIsReturned()
        {
            string rawJson = @"{ ""someProperty"": ""expectedValue"" }";
            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("someProperty");

            Assert.AreEqual("expectedValue", valueAtPath.Value<string>());
        }

        [TestMethod]
        public void InsensitiveToken_JsonPathHasOneLevelAndMissmatchedCasing_TheJTokenWithStringValueExpectedValueIsReturned()
        {
            string rawJson = @"{ ""someproperty"": ""expectedValue"" }";
            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("someProperty");

            Assert.AreEqual("expectedValue", valueAtPath.Value<string>());
        }

        [TestMethod]
        public void InsensitiveToken_JsonPathHasTwoLevelsAndMissmatchedCasing_TheJTokenWithStringValueExpectedValueIsReturned()
        {
            string rawJson = @"{ 
                ""firstLevel"": 
                {
                    ""secondlevel"": ""expectedValue"" 
                }
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("firstLevel.secondLevel");

            Assert.AreEqual("expectedValue", valueAtPath.Value<string>());
        }

        [TestMethod]
        public void InsensitiveToken_JsonPathHasAnArrayAndMissmatchedCasing_TheJTokenAtIndexZeroIsReturned()
        {
            string rawJson = @"{ 
                ""Array"": 
                [
                    { ""indexZero"": ""expectedValue"" }
                ]
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("array[0]");

            JObject expectedValue = new JObject
            {
                { "indexZero", "expectedValue" }
            };

            bool bothValuesAreEqual = JToken.DeepEquals(valueAtPath, expectedValue);

            Assert.IsTrue(bothValuesAreEqual);
        }

        [TestMethod]
        public void InsensitiveToken_PathIsReferencingAnOutOfBoundsIndexAndInsensitivePathNotFoundBehaviorIsNull_NullIsReturned()
        {
            string rawJson = @"{ 
                ""Array"": 
                [
                    { ""indexZero"": ""expectedValue"" }
                ]
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("array[1]");

            Assert.IsNull(valueAtPath);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void InsensitiveToken_PathIsReferencingAnOutOfBoundsIndexAndInsensitivePathNotFoundBehaviorIsError_ExceptionIsThrown()
        {
            string rawJson = @"{ 
                ""Array"": 
                [
                    { ""indexZero"": ""expectedValue"" }
                ]
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            parsedJson.InsensitiveToken("array[1]", InsensitivePathNotFoundBehavior.Error);
        }

        [TestMethod]
        public void InsensitiveToken_PathIsReferencingAnOutOfBoundsIndexAndInsensitivePathNotFoundBehaviorIsLastValid_ReturnedValueEqualsOriginalJson()
        {
            string rawJson = @"{ 
                ""Array"": 
                [
                    { ""indexZero"": ""expectedValue"" }
                ]
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken("array[1]", InsensitivePathNotFoundBehavior.LastValid);

            bool bothValuesAreEqual = JToken.DeepEquals(parsedJson, valueAtPath);

            Assert.IsTrue(bothValuesAreEqual);
        }

        [TestMethod]
        public void InsensitiveToken_PathIsNullAndInsensitivePathNotFoundBehaviorIsLastValid_ReturnedValueEqualsOriginalJson()
        {
            string rawJson = @"{ 
                ""Array"": 
                [
                    { ""indexZero"": ""expectedValue"" }
                ]
            }";

            JObject parsedJson = JObject.Parse(rawJson);

            JToken valueAtPath = parsedJson.InsensitiveToken(null, InsensitivePathNotFoundBehavior.LastValid);

            bool bothValuesAreEqual = JToken.DeepEquals(parsedJson, valueAtPath);

            Assert.IsTrue(bothValuesAreEqual);
        }
    }
}
