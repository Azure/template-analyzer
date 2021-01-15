# Template BPA JSON Rules
<a name="note"></a>***Note**: The ARM Template BPA is currently in development. All features that have yet to be implemented have been flagged with an asterisk [\*].*

## Overview
Template BPA rules are authored in JSON.  Each rule contains metadata about what's being evaluated (such as name, description, help information), along with the specifics of the evaluation itself.  Files consisting of multiple rules should contain an array of rule objects.

## Template BPA Rule Object
Here are the fields that make up a rule definition.
```javascript
{
    "name": "Rule Name",
    "description": "Brief description of what the rule is evaluating",
    "recommendation": "Guidance describing what should be done to fix the issue if a template violates the rule",
    "helpUri": "URI to find more detailed information about the rule and how to fix a template",
    "evaluation": { … } // The evaluation logic of the rule.  More details below.
}
```

### Guidelines for rule metadata
- The `name` should be kept short and simple, but it should uniquely identify what the rule is checking.
- The `recommendation` should provide clear but concise guidance on how to modify a template if the rule fails.  If some details are somewhat complex, or the rule takes a bit more to understand, add those details to a guide accessible at the URI in `helpUri`.
- The `helpUri` is optional, but it is good practice to include.  For built-in rules, this will point to a guide in the GitHub repository.

## Evaluation Object
The evaluation object is comprised of three basic properties.
```javascript
{
    "path": "JSON path to property to evaluate in template",
    "resourceType": "(optional) The Azure resource type this evaluation applies to",
    <operator>: // One of several kinds of operators, defined below
}
```

Evaluation of ARM templates is performed on the JSON representation of the template.  Therefore, evaluations operate on the JSON properties of the template.  Specifying the template property is done by specifying a JSON path for the `path` key.  This path can contain wildcards ('*') to select multiple paths to evaluate [[*]](#note).

Since most rules apply only to specific types of Azure resources, the `resourceType` property gives rule authors a shorthand to only evaluate those types of resources.  If `resourceType` is specified, the path specified in `path` becomes relative to the resource selected in the template.

The behavior of the `resourceType` property is to find a property called "resources" in the current scope that is an array of objects, look for a "type" property in each of the objects, and keep only the resources where the value of "type" matches the string in `resourceType`.  See [Scopes](#scopes) for more information on scopes.

## Operators
There are two kinds of operators: [value operators](#value-operators) and [structured operators](#structured-operators).  Value operators evaluate a single value, whereas structured operators are used to nest and combine multiple evaluations, each containing their own operator.

### Value Operators
These operators evaluate a specific JSON property in the template.  All operators are valid properties in the evaluation object, but only one operator can be present in the top level of the evaluation.  If multiple operators are necessary, a structured operator can be used to combine or nest the operators.  The type of value each operator expects is defined with each operator.  Each operator must be accompanied by a `path` in the evaluation object.

The examples given with the operators below will be in the context of the following JSON:
```javascript
{
    "$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#",
    "parameters": { },
    "variables": { },
    "resources": [
        {
            "type": "Microsoft.Compute/virtualMachines",
            "name": "myVmResource",
            "apiVersion": "2020-06-01",
            "properties": {
                "networkProfile": {
                    "networkInterfaces": [
                        {
                            "id": "[resourceId('Microsoft.Network/networkInterfaces', 'myNic')]"
                        }
                    ]
                },
                "osProfile": {
                    "computerName": "myVm",
                    "adminUsername": "myusername",
                    "adminPassword": null,
                }
            }
        }
    ],
    "outputs": {
         "numberOfResourcesDeployed": {
            "type": "integer",
            "value": 1
        }
    }
}
```

#### `Exists`
*Type: Boolean*

Evaluates a JSON path to determine if the specified path exists in the template and compares that result to the value associated with the property in the rule.  Results in `true` if the existence of the path is the same as the expected value in the rule; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.osProfile.linuxConfiguration",
    "exists": false // Evaluates to `true` because the path "properties.osProfile.linuxConfiguration" is not defined in the JSON, which is what the rule expects.
}
```

#### `HasValue`
*Type: Boolean*

Evaluates a JSON path to determine if the specified path has any value in the template other than `null` or empty string and compares that result to the value associated with the property in the rule.  Results in `true` if the value of the path matches the expectation in the rule; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.osProfile.adminPassword",
    "hasValue": false // Evaluates to `true` because the path "properties.osProfile.adminPassword" is defined in the JSON, but its value is `null` (does not have a value), which is what the rule expects.
}
```

#### `Equals`
*Type: Any basic JSON value (integer, float, string, bool, null)*

Tests the template value of the `path` to determine if it is equal to the value specified in the rule.
- If the type of the value of `equals` does not match the type of the value at `path`, this evaluates to `false` (except for integer and float, which can be compared with one another).  Otherwise, this behaves as expected for the given type.
- Evaluations on string types are case-insensitive.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "name",
    "equals": "MyVMResource" // Evaluates to `true` because the value of the path "name" is a string and case-insensitively matches the value in the rule.
}
```

#### `NotEquals`
*Type: Any basic JSON value (integer, float, string, bool, null)*

The logical inverse of `equals`.  Evaluations on incompatible types results in `true`.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.osProfile.adminPassword",
    "notEquals": "password" // Evaluates to `true` because the value of the path "properties.osProfile.adminPassword" is `null`, which does not match the value in the rule.
}
```

#### `Less` [[*]](#note)
*Type: number (integer, float)*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is less than the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "less": 1 // Evaluates to `false` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is not less than 1.
}
```

#### `LessOrEquals` [[*]](#note)
*Type: number (integer, float)*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is less than or equal to the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "lessOrEquals": 1 // Evaluates to `true` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is equal to 1.
}
```

#### `Greater` [[*]](#note)
*Type: number (integer, float)*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is greater than the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "greater": 0 // Evaluates to `true` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is greater than 0.
}
```

#### `GreaterOrEquals` [[*]](#note)
*Type: number (integer, float)*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is greater than or equal to the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "greaterOrEquals": 2 // Evaluates to `false` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is not greater than or equal to 1.
}
```

#### `Regex` [[*]](#note)
*Type: string*

Runs the regular expression in the specified value against the value of the `path` in the template.  All regular expressions are case-insensitive.  Evaluates to `true` if the regular expression is a match; `false` otherwise.  If the value in the template is not a string, this evaluates to `false`.

Note: `regex` replaces many common string-type conditions.  Examples:
- Contains "value" --> `"regex": "value"`
- Starts with "begin" --> `"regex": "^begin"`
- Ends with "end" --> `"regex": "end$"`


Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.osProfile.adminUsername",
    "regex": "admin" // Evaluates to `false` because "admin" is not contained in the value of the path "properties.osProfile.adminUsername".
}
```

#### `In` [[*]](#note)
*Type: array of basic JSON values (integer, float, string, bool, null)*

Evaluates the value of the `path` in the template using the `equals` operator for each value specified in the array.  If any results in `true`, `in` will evaluate to true; `false` otherwise.  All values in the array must be of the same type.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "apiVersion",
    "in": [
         "2020-06-01",
         "2019-12-01",
         "2019-07-01",
         "2019-03-01"
    ] // Evaluates to `true` because the value of "apiVersion" in the template ("2020-06-01") is equal to one of the values in the array specified for `in`.
}
```

### Structured Operators
These operators build up a structure of child evaluations, and therefore contain additional operators inside them.  These operators are not required to include a `path`.  If `resourceType` or `path` are specified, that becomes the scope for all evaluations nested inside the operator.  More information on [Scopes](#scopes) can be found below.

#### `AnyOf` [[*]](#note)
*Type: array of [Evaluation Object](#evaluation-object)s*

Performs a logical 'or' operation on the array of evaluation objects.  Evaluates to `true` if the result of any evaluation in the array is `true`; evaluates to `false` otherwise.

Example:
```javascript
{
    "anyOf": [
        {
            "resourceType": "Microsoft.Compute/virtualMachines",
            "path": "properties.osProfile.adminPassword",
            "hasValue": false // Evaluates to `false`
        },
        {
            "resourceType": "Microsoft.Compute/virtualMachines",
            "path": "properties.osProfile.adminUsername",
            "regex": "username" // Evaluates to `true`
        }
    ] // Evaluates to `true` because one of the expressions contained in "anyOf" resulted in `true`
}
```

#### `AllOf`
*Type: array of [Evaluation Object](#evaluation-object)s*

Performs a logical 'and' operation on the array of evaluation objects.  Evaluates to `true` if the result of all evaluations in the array is `true`; evaluates to `false` otherwise.

Example:
```javascript
{
    "anyOf": [
        {
            "resourceType": "Microsoft.Compute/virtualMachines",
            "path": "properties.osProfile.adminPassword",
            "hasValue": false // Evaluates to `false`
        },
        {
            "resourceType": "Microsoft.Compute/virtualMachines",
            "path": "properties.osProfile.adminUsername",
            "regex": "username" // Evaluates to `true`
        }
    ] // Evaluates to `false` because not all the expressions contained in "allOf" resulted in `true`
}
```

#### `Not` [[*]](#note)
*Type: [Evaluation Object](#evaluation-object)*

Performs a logical 'not' operation on the evaluation object.  Evaluates to `true` if the result of the evaluation in the value of `not` is `false`; evaluates to `false` otherwise.

Example:
```javascript
{
    "not": [
        {
            "resourceType": "Microsoft.Compute/virtualMachines",
            "path": "properties.osProfile.adminUsername",
            "regex": "admin" // Evaluates to `false`
        }
    ] // Evaluates to `true` because it's the logical 'not' of the sub-evaluation that is `false`
}
```

## Scopes
Each Evaluation object has a path scope which is inherited by child Evaluations.  If a `path` and/or `resourceType` is specified in a Structured Evaluation object, the `path` and `resourceType` of each child Evaluation start at the path determined in the parent.  Therefore, each `path` continues from the `path` specified in the parent.
For example, here's a simple illustration:
```javascript
{
    "resourceType": "Microsoft.Web/sites",
    "allOf": [
        {
            "path": "kind",
            "regex": "api$"
        }
    ]
}
```
 
The full path used by the 'regex' Evaluator would be "resources[*].kind" (limited to resources where "type" equals "Microsoft.Web/sites").
 
First, `resourceType` is used to select resources within the "resources[]" array.  Then, only those resources with the given type are considered.  Further, the `path` specified with `regex` continues from the path in the parent scope, appending ".kind" to the resources selection.
