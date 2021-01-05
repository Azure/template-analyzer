# Contributing to ARMory
We welcome community contributions to ARMory. Please note that by participating in this project, you agree to abide by the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/)  and terms of the [CLA](#contributor-license-agreement-(cla)).

## Getting Started
* If you haven't already, you will need [dotnet core sdk 3.1](https://dotnet.microsoft.com/download) (or later) installed locally to build and run this project.
* Fork this repo (see [this forking guide](https://guides.github.com/activities/forking/) for more information).
* Checkout the repo locally with `git clone git@github.com:{your_username}/armory.git`.
* Build the .NET solution with `dotnet build`.
 
## Developing
 
### Components
The ARMory solution is comprised of the following main components:
* ARMory CLI (*[src\Armory.Cli](./src/Armory.Cli)*): The command-line tool to execute ARMory. This executable will pass template files to ARMory.Core.
* ARMory Core (*[src\Armory.Core](./src/Armory.Core)*): The main ARMory library which executes all rule engines against provided templates.
  * BuiltInRules.json (*[src\Armory.Core\Rules\BuiltInRules.json](./src/Armory.Core/Rules/BuiltInRules.json)*): The file with the built-in ARMory rules.
* ARMory Template Processor (*[src\Armory.TemplateProcessor](./src/Armory.TemplateProcessor)*): This library parses ARM templates and evaluates expressions found in the template.
* ARMory JSON Rule Engine (*[src\Armory.JsonRuleEngine](./src/Armory.JsonRuleEngine)*): The library dedicated to parse and evaluate ARMory JSON rules.
 
### Code Structure
1. ARMory CLI (or another calling application) identifies JSON files (template and parameter files) and invokes ARMory.Core.
2. ARMory Core calls the ARMory Template Processing Library to process the template and (if supplied) the provided parameters. The ARMory Template Processing Library processes all the template functions.
3. ARMory Core then calls the JSON Rule Engine and evaluates each rule against the template/parameter pairs.
4. JSON Rule Engine evaluates the expressions specified in the `evaluation` section of the rule and generates results to identify the rule violation in the template.
 
### Running the tests
Use `dotnet test` to run the full ARMory test suite

### Contributing Rules
Review the [Authoring JSON Rules](./docs/authoring-json-rules.md) section to contribute to the built-in ARMory rules.

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
    /// Runs the ARMory logic given the template and parameters passed to it
    /// </summary>
    /// <param name="template">The ARM Template <c>JSON</c>. Must follow this schema: https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#</param>
    /// <param name="parameters">The parameters for the ARM Template <c>JSON</c></param>
    /// <returns>List of ARMory results</returns>
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
