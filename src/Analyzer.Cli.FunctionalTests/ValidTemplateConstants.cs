using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Analyzer.Cli.FunctionalTests
{
    public class ValidTemplateConstants
    {
        public const string MissingStartObject = @" ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""}";

        public const string NoValidTopLevelProperties =@"
            { 
              ""parameters"": {
              ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
            }    
        }";

        public const string CaseSensitivity = @"{""$schEmA"": ""https://ScHeMa.mAnAgEmEnt.AzUrE.cOm/schEmAs/2019-04-01/deploymentTemplate.json#""}";

        public const string MissingSchema = @"{""contentVersion"": ""1.0.0.0"", ""Parameters"": {}}";

        public const string SchemaValueNotString = @"{ ""$schema"": 5 }";

        public const string NoSchemaInvalidProperties = @" {""properties"": }";

        public const string DifferentSchemaDepths = @"
        {
            ""parameters"": ""{
              ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
                },
             ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
        }";

        public const string InvalidTopLevelProperties = @"{
            ""{"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#""
        }";

        public const string PassingTest = @"{
            ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#"",
            ""contentVersion"": ""1.0.0.0"",
            ""parameters"": {},
            ""functions"": [],
            ""variables"": {},
            ""resources"": [],
            ""outputs"": {}
        }";  
    }
}
