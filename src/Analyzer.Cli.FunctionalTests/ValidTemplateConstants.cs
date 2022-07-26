namespace Analyzer.Cli.FunctionalTests
{   
    /// <summary>
    /// Constants being passed in as test cases for the CommandLineParserTests.
    /// </summary>
    public class TestcaseTemplateConstants
    {
        // Pass
        public const string PassingTest = @"{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {},
            ""functions"": [],
            ""variables"": {},
            ""resources"": [],
            ""outputs"": {}
        }";

        // Pass
        public const string SchemaCaseInsensitive = @"{""$schEmA"": ""https://ScHeMa.mAnAgEmEnt.AzUrE.cOm/schEmAs/2019-04-01/deploymentTemplate.json#"", ""contentVersion"": ""1.0.0.0"", ""resources"": [] }";

        // Pass
        public const string DifferentSchemaDepths = @"{
            ""parameters"": {},
            ""functions"": [],
            ""variables"": {""$schema"": ""Invalid Schema""},
            ""resources"": [],
            ""outputs"": {},
            ""contentVersion"": ""1.0.0.0"",
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
        }";

        // Fail
        public const string MissingStartObject = @" ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""}";
        
        // Fail
        public const string NoValidTopLevelProperties = @"
            { 
              ""parameters"": {
              ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
            }    
        }";

        // Fail
        public const string MissingSchema = @"{""contentVersion"": ""1.0.0.0"", ""Parameters"": {}}";

        // Fail
        public const string SchemaValueNotString = @"{ ""$schema"": 5 }";

        // Fail
        public const string NoSchemaInvalidProperties = @" {""properties"": }";
    }
}