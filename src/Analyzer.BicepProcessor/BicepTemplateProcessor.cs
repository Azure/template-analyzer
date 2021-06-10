using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    public class BicepTemplateProcessor
    {

        private readonly string bicep;
        private readonly string apiVersion;
        private readonly bool dropResourceCopies;

        /// <summary>
        ///  Constructor for the ARM Template Processing library
        /// </summary>
        /// <param name="bicepTemplate">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="apiVersion">The deployment API version. Must be a valid version from the deploymetns list here: https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/allversions</param>
        /// <param name="dropResourceCopies">Whether copies of resources (when using the copy element in the ARM Template) should be dropped after processing.</param>
        public BicepTemplateProcessor(string bicepTemplate, string apiVersion = "2020-01-01", bool dropResourceCopies = false)
        {
            this.bicep = bicepTemplate;
            this.apiVersion = apiVersion;
            this.dropResourceCopies = dropResourceCopies;
        }

        public ArmTemplateProcessor ToArmTemplateProcessor() 
        {
            string template = ConvertBicepToJson();
            ArmTemplateProcessor armTemplateProcessor = new ArmTemplateProcessor(template);
            return armTemplateProcessor;
        }

        private string ConvertBicepToJson()
        {
            // TODO: compile bicep into arm json
            return this.bicep;
        }

    }

}
