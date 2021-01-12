[![Build Status](https://dev.azure.com/azure/armory/_apis/build/status/Azure.armory?branchName=main)](https://dev.azure.com/azure/armory/_build/latest?definitionId=91&branchName=main)

# ARM Template Best Practice Analyzer (BPA)
***Note**: The Template BPA is currently in development. It is not yet recommended for production usage.*
 
## What is the ARM Template BPA?
[Azure Resource Manager (ARM) templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) – Infrastructure-as-code (IaC) for your Azure solutions – are JSON files that define the infrastructure and configuration for your Azure deployments. The Template BPA is an ARM template security validator that scans ARM templates and checks that security best practices are being followed before deployment. 
 
The Template BPA provides a simple and extensible solution to improve the security of your Azure resources before deployment and ensures your ARM templates follow best practices. The Template BPA checks are customizable. You can write your own checks and enforce only the checks that are relevant for your business. 
 
 
## Getting started with the Template BPA
Since the Template BPA is still under development, this project can only be run from source code. Please `clone` the project and use your IDE/text editor of choice that is compatible with .NET.

Detailed documentation can be found in [docs](./docs).
 
## Using the Template BPA
 
### Input
The Template BPA depends on a few inputs – the Template BPA rules, ARM template, and (optional) ARM template parameters. The Template BPA rules are written in JSON. You can add and modify what rules to run. See the [documentation for more information about how to author the Template BPA JSON rules](./docs/authoring-json-rules.md). The Template BPA then runs the configured rules against the provided [ARM templates]( https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/) and their corresponding [template parameters](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/parameter-files) if specified. If no template parameters are specified, then the Template BPA generates the minimum number of placeholder parameters to properly evaluate [template functions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions) in the ARM templates. **Note**: Providing the Template BPA with template parameter values will result in more accurate results as it will more accurately represent your deployments. The values provided to parameters may affect the evaluation of the Template BPA rule, altering its results. That said, **do not** save sensitive data (passwords, connection strings, etc.) in parameter files in your repositories. Instead, [retrieve these values from  your ARM template from Azure Key Vault](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/key-vault-parameter?tabs=azure-cli#reference-secrets-with-static-id). 

### Output
The Template BPA outputs the results of violated rules, its corresponding line number, and the recommendation to remediate that violation.
 
## Contributing
This project welcomes contributions and suggestions. Please see the [Contribution Guide](./CONTRIBUTING.md) for more details about how to contribute to the Template BPA. Most contributions require you to
agree to a Contributor License Agreement (CLA) declaring that you have the right to,
and actually do, grant us the rights to use your contribution. For details, visit
https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need
to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the
instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general). Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.