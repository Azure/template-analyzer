// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Constants
{
    /// <summary>
    /// Defines all constants used in JsonEngine
    /// </summary>
    internal class JsonRuleEngineConstants
    {
        internal const string ActualValuePlaceholder = "{actualValue}";
        internal const string PathPlaceholder = "{path}";
        internal const string ExpectedValuePlaceholder = "{expectedValue}";
        internal const string NegationPlaceholder = "{negation}";

        internal static string EqualsFailureMessage = $"Value \"{ActualValuePlaceholder}\" found at \"{PathPlaceholder}\" is {NegationPlaceholder} equal to \"{ExpectedValuePlaceholder}\".";
        internal static string ExistsFailureMessage = $"Value found at \"{PathPlaceholder}\" exists: {ActualValuePlaceholder}, expected: {ExpectedValuePlaceholder}.";
        internal static string HasValueFailureMessage = $"Value found at \"{PathPlaceholder}\" has a value: {ActualValuePlaceholder}, expected: {ExpectedValuePlaceholder}.";
        internal static string RegexFailureMessage = $"Value \"{ActualValuePlaceholder}\" found at \"{PathPlaceholder}\" does not match regex pattern: \"{ExpectedValuePlaceholder}\".";
        internal static string InFailureMessage = $"Value \"{ExpectedValuePlaceholder}\" is not in the list at path \"{PathPlaceholder}\".";
    }
}
