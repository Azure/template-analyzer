[![Build Status](https://dev.azure.com/azure/template-analyzer/_apis/build/status/BuildAndTest?branchName=main)](https://dev.azure.com/azure/template-analyzer/_build/latest?definitionId=91&branchName=main)
[![Code Coverage](https://shields.io/azure-devops/coverage/azure/template-analyzer/91)](https://dev.azure.com/azure/template-analyzer/_build/latest?definitionId=91&branchName=main)

# ARM Template Best Practice Analyzer (BPA)
***Note**: The Template BPA is currently in development. It is not yet recommended for production usage.*

## What is the ARM Template BPA?
[Azure Resource Manager (ARM) templates](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/overview) – Infrastructure-as-code (IaC) for your Azure solutions – are JSON files that define the infrastructure and configuration for your Azure deployments. The Template BPA is an ARM template validator that scans ARM templates to ensure security and best practice checks are being followed before deployment.

The Template BPA provides a simple and extensible solution to improve the security of your Azure resources before deployment and ensures your ARM templates follow best practices. The Template BPA is designed to be customizable - users can write their own checks and/or enforce only the checks that are relevant for them.

## Getting started with the Template BPA
The Template BPA is built using .NET 5.  Having the [.NET 5 Runtime](https://dotnet.microsoft.com/download) installed is currently a prerequisite.

After ensuring the .NET Runtime is installed, download the latest Template BPA release in [the releases section](https://github.com/Azure/template-analyzer/releases).

To preview the rules that come bundled with the Template BPA, explore [the built-in rules](docs/built-in-bpa-rules.md).

## Using the Template BPA
The Template BPA is executed via a command line.  Here are the formats to invoke it:

`TemplateAnalyzer.exe analyze-template <template-path> [-p <parameters-path>]`

`TemplateAnalyzer.exe analyze-directory <directory-path>`

### Input
The Template BPA accepts the following inputs:

Argument | Description
--- | ---
`<template-path>` | The ARM template to analyze
`<directory-path>` | The directory to find ARM templates (recursively finds all templates in the directory and its subdirectories.)
**(Optional)** `-p` or `--parameters-file-path` | A [parameters file](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/parameter-files)
**(Optional)** `--report-format` | <p>*Console*: output results to the console in plain text. **(default)**</p>*Sarif*: output results to a file in [SARIF](https://sarifweb.azurewebsites.net) format.
`-o` or `--output-file-path` | **(Required if `--report-format` is *Sarif*)**  File path to output SARIF results to.

 The Template BPA runs the [configured rules](#understanding-and-customizing-rules) against the provided ARM template and its corresponding [template parameters](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/parameter-files), if specified. If no template parameters are specified, then the Template BPA generates the minimum number of placeholder parameters to properly evaluate [template functions](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/template-functions) in the ARM template.

**Note**: Providing the Template BPA with template parameter values will result in more accurate results as it will more accurately represent your deployments. The values provided to parameters may affect the evaluation of the Template BPA rule, altering its results. That said, **DO NOT** save sensitive data (passwords, connection strings, etc.) in parameter files in your repositories. Instead, [retrieve these values from  your ARM template from Azure Key Vault](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/key-vault-parameter?tabs=azure-cli#reference-secrets-with-static-id).

### Output
Results can be output in plain text to the console, or output to a file in SARIF format. Template BPA will exit with an error code if any errors or violations are found during a scan.

#### Console
The Template BPA outputs the results of violated rules, the corresponding line numbers where rules failed, and a recommendation to remediate each violation.

For a template which deploys an API App that does not require HTTPS, running the Template BPA on the template would produce output which looks similar to the following:
```
>TemplateAnalyzer.exe analyze-template "C:\Templates\azuredeploy.json"

File: C:\Templates\azuredeploy.json

        AppServiceApiApp_HTTPS: API App should only be accessible over HTTPS
                More information: https://github.com/Azure/template-analyzer/blob/main/docs/built-in-bpa-rules.md#api-app-should-only-be-accessible-over-https
                Result: Failed
                Line: 114

        Rules passed: 25
```

#### SARIF
Results are written to the file specified (with the `-o` or `--output-file-path` argument) in [SARIF](https://sarifweb.azurewebsites.net) format.

#### Exit codes
| Scenario      | Exit Code |
| ----------- | ----------- |
| Success: Operation was successful | 0 |
| Error: GenericError | 1 |
| Error: Invalid file path | 2 |
| Error: Missing file path | 3 |
| Error: Invalid ARM template | 4 |
| Issue: Scan found rule violations | 5 |
| Error + Issue: Scan has both errors and violations | 6 |

### Understanding and customizing rules
The analysis rules used by the Template BPA are written in JSON, located in *Rules/BuiltInRules.json* (starting from the directory *TemplateAnalyzer.exe* is in). This file can be added to and/or modified to change the rules that are run. See the [documentation for more information about how to author Template BPA JSON rules](./docs/authoring-json-rules.md).

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
