# Contributing to the ARM Template BPA
We welcome community contributions to the Template BPA. Please note that by participating in this project, you agree to abide by the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/)  and terms of the [CLA](#contributor-license-agreement-cla).

## Getting Started
* If you haven't already, you will need [dotnet core sdk 3.1](https://dotnet.microsoft.com/download) (or later) installed locally to build and run this project.
* Fork this repo (see [this forking guide](https://guides.github.com/activities/forking/) for more information).
* Checkout the repo locally with `git clone git@github.com:{your_username}/template-analyzer.git`.
* Build the .NET solution with `dotnet build`.
 
## Developing
 
### Components
The Template Analyzer solution is comprised of the following main components:
* CLI (*[src\Analyzer.Cli](./src/Analyzer.Cli)*): The command-line tool to execute the Template BPA. This executable will pass template files to Analyzer.Core.
* Core (*[src\Analyzer.Core](./src/Analyzer.Core)*): The main Analyzer library which executes all rule engines against provided templates.
  * BuiltInRules.json (*[src\Analyzer.Core\Rules\BuiltInRules.json](./src/Analyzer.Core/Rules/BuiltInRules.json)*): The file with the built-in Template BPA rules.
* Template Processor (*[src\Analyzer.TemplateProcessor](./src/Analyzer.TemplateProcessor)*): This library parses ARM templates and evaluates expressions found in the template.
* JSON Rule Engine (*[src\Analyzer.JsonRuleEngine](./src/Analyzer.JsonRuleEngine)*): The library dedicated to parse and evaluate the Template BPA JSON rules.
 
### Code Structure
1. Analyzer CLI (or another calling application) identifies JSON files (template and parameter files) and invokes Analyzer.Core.
2. Analyzer Core calls the Template Processing Library to process the template and (if supplied) the provided parameters. The Template Processing Library processes all the template functions.
3. Analyzer Core then calls the JSON Rule Engine and evaluates each rule against the template/parameter pairs.
4. JSON Rule Engine evaluates the expressions specified in the `evaluation` section of the rule and generates results to identify the rule violation in the template.
 
### Running the tests
Use `dotnet test` to run the full Template BPA test suite

### Contributing Rules
Review the [Authoring JSON Rules](./docs/authoring-json-rules.md) section to contribute to the built-in Template BPA rules.

### Coding Conventions

#### Code
Please follow the below conventions when contributing to this project.
* Using directives:
  * Should be listed alphabetically by namespace
  * `System.*` namespaces should be listed first
  * Namespaces can be grouped by top-level name, but should be alphabetical within the group
* Follow the below naming conventions:

    | Components | Casing |
    | --- | --- |
    | Class names, properties, and methods | *PascalCase* |
    | Private class variables | *camelCase* |

* All internal and public classes, methods, and properties should include a `/// <summary>` comment 
  * Add `<param>` and `<returns>` when applicable
  * **Example**:
    ``` C#
    /// <summary>
    /// Runs the TemplateAnalyzer logic given the template and parameters passed to it
    /// </summary>
    /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
    /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
    /// <returns>List of TemplateAnalyzer results</returns>
    public static IEnumerable<IResult> Run(string template, string parameters = null)
    {
        …
    }
    ```
#### Tests
High test code coverage is required to contribute to this project. This ensures the highest code quality. **80% test code coverage is required.** This project uses Microsoft.VisualStudio.TestTools.UnitTesting for its tests. 
Please follow the below conventions when contributing to this project.
* Each .NET project should have its corresponding test project
* Each class should have its corresponding test class
* Each internal and public function in the class should be tested in the unit test class
* Follow the below naming conventions
  * Test Project: *{project name}.(Unit/Functional)Tests* selecting the appropriate type of tests found in the project
  * Test Class: *{class name}Tests.cs*
  * Test methods:
    * (Data)TestMethod: `{method name}_{what is being tested}_{expected outcome}`
    * DataRow (display name): Short description that clearly differentiate between the DataRows

## Contributor License Agreement (CLA)
This project welcomes contributions and suggestions. Most contributions require you to
agree to a Contributor License Agreement (CLA) declaring that you have the right to,
and actually do, grant us the rights to use your contribution. For details, visit
https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need
to provide a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the
instructions provided by the bot. You will only need to do this once across all repositories using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/)
or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
