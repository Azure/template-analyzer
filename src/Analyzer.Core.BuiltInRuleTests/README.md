# Test Project for Built-in Rules
This project is designed for quickly adding tests to verify the correctness of built-in rules.  When new rules are added, tests should be added here to make sure the rule is written correctly and help protect against incorrect changes to the rule.

## Test Setup
A test (or tests) for a given rule consists of 2 parts (each part is described in more detail below):
1. A new directory, containing a new JSON configuration describing the test(s) - the name of both the directory and the JSON configuration must match the ID of the rule it will test.
2. One or more test ARM templates to analyze.  These templates must be within the same directory the test configuration file resides in.  The results of analyzing these templates are compared with the test configuration to assert correctness of the rule.

### JSON Configuration
To create a set of tests for a rule, a new directory and JSON file are created.  **The name of the directory and file must be the same as the `id` property of the JSON rule**, with ".json" as the file extension.

For example, to write tests for rule ID *TA-000001*, a test configuration file must be located at *TA-000001/TA-000001.json* within the *Tests* directory.

The JSON test configuration has the following schema:
``` js
[
    {
        "Template": "Name of template file analyzed.  Template must be within the same directory as the test configuration file.",
        "ReportedFailures": [ // Array of objects with integer line numbers - each are a line number expected to be reported in the failure.
            {
                "LineNumber": 3, // Line number of expected reported failure
                "Description": "(Optional) Description of what's being tested for this expected failure."
            }
        // Other optional properties can be specified as well, for example to give context on why some resources are expected to pass analysis.
        ]
    },
    ... // More tests can be defined if multiple templates should be analyzed - one test block for each template
]
```

### Test ARM Templates
For each template file referenced in a `Template` property of a test configuration, there should be a file within the same directory with the same name.

The template can define anything needed to test the rule, but it must at least be a valid ARM template that can be parsed so analysis can be run on it.

## Test Execution
If running tests in Visual Studio Code, these tests will execute as part of running the 'test' task.  They also run as part of executing `dotnet test`.  This test project can be executed by itself with `dotnet test Analyzer.Core.JsonRuleTests\Analyzer.Core.JsonRuleTests.csproj`.
