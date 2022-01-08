# Test Project for the Configuration File
This project is designed for quickly adding tests to verify the correctness of the configuration file.  When new properties are added to the configuration file, tests should be added here to make sure the configuration file is written correctly and help protect against incorrect changes to the configuration file.

## Test Setup
A test (or tests) for a given configuration file consists of 3 parts (each part is described in more detail below):
1. A JSON configuration describing the test(s).  This configuration is in the *Tests* directory.
2. A JSON configuration describing the configuration(s).  This file is in the *TestConfigurations* directory.
3. One or more test JSON configuration files to analyze.  These files are in the *TestTemplates* directory.
The Template Analyzer is run against the test configuration files with the configuration file. The results of the analysis are compared with the test configuration to assert correctness of the configuration file analyzer.

### JSON Test Configuration
To create a set of tests for a rule, a new JSON file is created in the *Tests* directory.

The JSON test configuration has the following schema:
``` js
[
    {
        "Template": "Name of template file analyzed (without file extension).  Template must be in the 'TestTemplates' directory.",
        "Configuration": "Name of configuration file analyzed (without file extension).  File must be in the 'TestConfigurations' directory.",
        "ReportedFailures": [ // Array of objects with expected Id and their respective Severity to be reported in the failure.
            {
                "Id": TA-000003, // Id of expected reported failure
                "Severity": 1 // Severity of expected reported failure
            }
        ]
    },
    ... // More tests can be defined if multiple templates should be analyzed - one test block for each template
]
```

### JSON Configuration File
To create a set of tests for a rule, a new JSON file is created in the *TestConfigurations* directory.

The JSON test configuration file follows the schema described (here)[https://github.com/Azure/template-analyzer/blob/main/docs/customizing-evaluation-outputs].


### Test ARM Templates
For each template file referenced in a `Template` property of a test configuration, there should be a file in the *TestTemplates* directory with the same name, **having ".badtemplate" as the file extension**.  (This extension is used to help prevent these templates from actually being deployed in Azure.)

For example, if the value of `Template` is "SuperSecurityCheck_failure", there is expected to be a template file at path *TestTemplates/SuperSecurityCheck_failure.badtemplate*.

The template can define anything needed to test the rule, but it must at least be a valid ARM template that can be parsed so the analyzer can run on it.

## Test Execution
If running tests in Visual Studio Code, these tests will execute as part of running the 'test' task.  They also run as part of executing `dotnet test`.
