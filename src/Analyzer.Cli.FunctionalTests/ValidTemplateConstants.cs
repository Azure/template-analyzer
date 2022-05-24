using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Cli.FunctionalTests
{   /// <summary>
/// Constants being passed in as test cases, first three contants should pass while the following five should fail.
/// </summary>
    public class ValidTemplateConstants
    {
        public const string PassingTest = @"{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {},
            ""functions"": [],
            ""variables"": {},
            ""resources"": [],
            ""outputs"": {}
        }";

        public const string SchemaCaseInsensitive = @"{""$schEmA"": ""https://ScHeMa.mAnAgEmEnt.AzUrE.cOm/schEmAs/2019-04-01/deploymentTemplate.json#"", ""contentVersion"": ""1.0.0.0"", ""resources"": [] }";

        public const string DifferentSchemaDepths = @"{
            ""parameters"": {},
            ""functions"": [],
            ""variables"": {""$schema"": ""https://schema.management.azure.com/schemas/2040-40-40/deploymentTemplate.json#""},
            ""resources"": [],
            ""outputs"": {},
            ""contentVersion"": ""1.0.0.0"",
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
        }";

        public const string MissingStartObject = @" ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""}";

        public const string NoValidTopLevelProperties = @"
            { 
              ""parameters"": {
              ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
            }    
        }";

        public const string MissingSchema = @"{""contentVersion"": ""1.0.0.0"", ""Parameters"": {}}";

        public const string SchemaValueNotString = @"{ ""$schema"": 5 }";

        public const string NoSchemaInvalidProperties = @" {""properties"": }";
    }
}
