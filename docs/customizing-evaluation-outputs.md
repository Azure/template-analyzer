# Customizing Evaluation Outputs

## Overview
Using an additional JSON configuration file, Template Analyzer can be customized in how it runs **JSON-based rules**. For example, specific rules can be included in/excluded from execution, or the severity of a rule can be changed. See the main README for details on [how to run Template Analyzer with a configuration file](https://github.com/Azure/template-analyzer#using-the-template-bpa).

## Template BPA Rule Object
Here are the fields that make up a custom configuration file:
```javascript
{
    "exclusions": { ... }, // Parameters that are excluded from the BPA execution. More details below.
    "inclusions": { ... }, // Only parameters that will be included in the BPA execution. More details below.
    "severityOverrides": { ... } // Key-value pairs of Id and new severity value. Can change a rule's severity.
}
```

### Exclusions Object
The `exclusions` object is comprised of the following optional properties:
```javascript
"exclusions": {
    "severity": [int], // List of severities to exclude from results. Any rules with matching severities will be omitted from results.
    "ids": ["Id"] // List of ids to exclude from results. Any rules with matching ids will be omitted from results.
}
```
**Note**: Rules defined in the exclusions object will be omitted from results. If the inclusions object contains values, then the exclusions object will be ignored. 

### Inclusions Object
The `inclusions` object is comprised of the following optional properties:
```javascript
"inclusions": {
    "severity": [int], // List of severities to include in results.
    "ids": ["Id"] // List of ids to include in results. 
}
```
**Note**: Only rules with matching severities or ids will be included in results. _If the inclusions object contains values, then the exclusions object will be ignored._

### SeverityOverrides Object 
The `severityOverrides` is composed of key-value pairs of id and new severity value. Below is an example of changing the severities on two rules:
```javascript
"severityOverrides": {
    "TA-000001": 3,
    "TA-000007": 3
}
```
**Note**: The `severityOverrides` object is evaluated after the `inclusions/exclusions` object. For example, if the configuration looks like the below:
```javascript
"inclusions": {
    "severity": 2
},
"severityOverrides": {
    "TA-000001": 3
}
```
where rule id "TA-000001" was originally a severity `2`, the result of this evaluation will include all original severity 2 rules and "TA-000001" as a severity 3 rule. 
