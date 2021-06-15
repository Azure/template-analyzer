using Microsoft.Azure.Templates.Analyzer.TemplateProcessor;
using System.IO;
using Bicep.Core.Emit;
using Bicep.Core.FileSystem;
using Bicep.Core.Semantics;
using Bicep.Core.Syntax;
using Bicep.Core.Workspaces;
using Bicep.Core.TypeSystem.Az;

namespace Microsoft.Azure.Templates.Analyzer.BicepProcessor
{
    public class BicepTemplateProcessor
    {

        private readonly string bicep;
        private readonly string bicepPath;
        private readonly string apiVersion;
        private readonly bool dropResourceCopies;

        /// <summary>
        ///  Constructor for the ARM Template Processing library
        /// </summary>
        /// <param name="bicepTemplate">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="bicepPath">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
        /// <param name="apiVersion">The deployment API version. Must be a valid version from the deploymetns list here: https://docs.microsoft.com/en-us/azure/templates/microsoft.resources/allversions</param>
        /// <param name="dropResourceCopies">Whether copies of resources (when using the copy element in the ARM Template) should be dropped after processing.</param>
        public BicepTemplateProcessor(string bicepTemplate, string bicepPath, string apiVersion = "2020-01-01", bool dropResourceCopies = false)
        {
            this.bicep = bicepTemplate;
            this.bicepPath = bicepPath;
            this.apiVersion = apiVersion;
            this.dropResourceCopies = dropResourceCopies;
        }
        public string ConvertBicepToJson()
        {
            using var stringWriter = new StringWriter();
            var syntaxTreeGrouping = SyntaxTreeGroupingBuilder.Build(new FileResolver(), new Workspace(), PathHelper.FilePathToFileUrl(bicepPath));
            var compilation = new Compilation(AzResourceTypeProvider.CreateWithAzTypes(), syntaxTreeGrouping);
            var emitter = new TemplateEmitter(compilation.GetEntrypointSemanticModel(), "0.4.123.62267");
            emitter.Emit(stringWriter);
            return stringWriter.ToString();
        }

    }

}
