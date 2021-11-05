# Template BPA Customizing Evaluation Outputs
<a name="note"></a>***Note**: The ARM Template BPA is currently in development. All features that have yet to be implemented have been flagged with an asterisk [\*].*

## Overview
Template BPA customization is authored in JSON.  Custom evaluation settings are written in a separate configuration and used on tool executing. More details on how to run Template BPA with a configuration file can be found [here](https://github.com/Azure/template-analyzer#using-the-template-bpa). 

## Template BPA Rule Object
Here are the fields that make up a custom configuration file.
```javascript
{
    "exclusions": { � }, // Parameters that are excluded from the BPA execution.  More details below.
    "inclusions": { � }, // Only parameters that will be included in the BPA execution.  More details below.
    "output": { � }, // How the BPA execution output will be displayed to the user.  More details below.
    "severityReMapping": { � } // Tuple of ruleId and new severity value.  Can change a rule's severity.
}
```

### Guidelines for exclusions object
The `exclusions` is comprised of the following optional properties.
```javascript
"exclusions": {
    "severity": [int], // Means don't include rule with servities that are in this list in the BPA execution
    "ruleIds": ["ruleId"] // Means don't include rules that are in this list in the BPA execution
}
```

### Guidelines for inclusions object
The `inclusions` is comprised of the following optional properties.
```javascript
"inclusions": {
    "ruleIds": ["ruleId"] // Means only include rules that are in this list in the BPA execution
}
```

### Guidelines for output object [[*]](#note)
**NOTE: `Output` is not yet supported. 
The `output` is comprised of the following optional properties.
```javascript
"output" : {
    "file": boolean, 
    "sortBy": ["higherSev", "alphabetic"]
}
```

### Guidelines for severityReMapping tuple
The `severityReMapping` is composed of a tuple of ruleId and new severity value. Below is an example of changing the severities on two rules.
```javascript
"severityReMapping": {
    "TA-000001": 3,
    "TA-000007": 3
}
```