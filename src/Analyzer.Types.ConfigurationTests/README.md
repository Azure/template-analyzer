# Test Project for the Configuration File
This project is designed for quickly adding tests to verify the correctness of the configuration file.  When new properties are added to the configuration file, tests should be added here to make sure the configuration file is written correctly and help protect against incorrect changes to the configuration file.

## Test Setup
A test (or tests) for a given configuration file consists of 2 parts (each part is described in more detail below):
1. A JSON configuration describing the test(s).  This configuration is in the *Tests* directory.
2. One or more test JSON configuration files to analyze.  These files are in the *TestFiles* directory.
The Template Analyzer is run against the test configuration files. The results of the analysis are compared with the test configuration to assert correctness of the configuration file analyzer.

# TODO update below

### JSON Configuration
To create a set of tests for a rule, a new JSON file is created in the *Tests* directory.  **The name of the file must be the same as the `name` property of the JSON rule**, with ".json" as the file extension.

For example, a JSON rule like the following:
``` js
{
    "name": "SuperSecurityCheck",
    "description": "...",
    "recommendation": "...",
    "helpUri": "...",
    "evaluation": {
      ...
    }
}
```
... would have tests defined in a configuration file named *"Tests/SuperSecurityCheck.json"*.

The JSON test configuration has the following schema:
``` js
[
    {
        "Template": "Name of template file analyzed (without file extension).  Template must be in the 'TestTemplates' directory.",
        "ReportedFailures": [ // Array of objects with integer line numbers - each are a line number expected to be reported in the failure.
            {
                "LineNumber": 3, // Line number of expected reported failure
                "Description": "(Optional) Description of what's being tested for this expected failure."
            }
        ]
    },
    ... // More tests can be defined if multiple templates should be analyzed - one test block for each template
]
```

### Test ARM Templates
For each template file referenced in a `Template` property of a test configuration, there should be a file in the *TestTemplates* directory with the same name, **having ".badtemplate" as the file extension**.  (This extension is used to help prevent these templates from actually being deployed in Azure.)

For example, if the value of `Template` is "SuperSecurityCheck_failure", there is expected to be a template file at path *TestTemplates/SuperSecurityCheck_failure.badtemplate*.

The template can define anything needed to test the rule, but it must at least be a valid ARM template that can be parsed so the analyzer can run on it.

## Test Execution
If running tests in Visual Studio Code, these tests will execute as part of running the 'test' task.  They also run as part of executing `dotnet test`.  This test project can be executed by itself with `dotnet test Analyzer.Core.JsonRuleTests\Analyzer.Core.JsonRuleTests.csproj`.
