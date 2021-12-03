# Template BPA Customizing Evaluation Outputs
<a name="note"></a>***Note**: The ARM Template BPA is currently in development. All features that have yet to be implemented have been flagged with an asterisk [\*].*

## Overview
Template BPA customization is authored in JSON.  Custom evaluation settings are written in a separate configuration and used on tool execution. See the main README for details on [how to run Template BPA with a configuration file](https://github.com/Azure/template-analyzer#using-the-template-bpa). 

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
    "severity": [int], // List of severities to not include in results. Any rules with matching severities will be omitted from results
    "ids": ["Id"] // List of ids to not include in results. Any rules with matching ids will be omitted from results
}
```
If the inclusions object contains values, then the exclusions object will be ignored. 

### Inclusions Object
The `inclusions` object is comprised of the following optional properties:
```javascript
"inclusions": {
    "ids": ["Id"] // List of ids to only include in results. Any ids not in the list will be omitted from results
}
```
If the inclusions object contains values, then the exclusions object will be ignored. 

### SeverityOverrides Tuple [[*]](#note)
**NOTE: `severityOverrides` is not yet supported. 
The `severityOverrides` is composed of a key-value pairs of id and a new severity value. Below is an example of changing the severities on two rules:
```javascript
"severityOverrides": {
    "TA-000001": 3,
    "TA-000007": 3
}
```