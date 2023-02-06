# Authoring Template Analyzer JSON Rules
<a name="note"></a>***Note**: All features that have yet to be implemented have been flagged with an asterisk [\*].*

## Overview
Template Analyzer built-in rules are authored in JSON.  Each rule contains metadata about what's being evaluated (such as id, description, help information, severity), along with the specifics of the evaluation itself.  Files consisting of multiple rules should contain an array of rule objects.

## Rule Object
Here are the fields that make up a rule definition.
```javascript
{
    "id": "Rule id",
    "name": "A human-readable identifier"
    "shortDescription": "Brief description of what the rule is evaluating",
    "fullDescription": "Detailed description of what the rule is evaluating",
    "recommendation": "Guidance describing what should be done to fix the issue if a template violates the rule",
    "helpUri": "URI to find more detailed information about the rule and how to fix a template",
    "severity" : "Integer value between 1 and 3, with 1 being high and 3 being low, designating the importance of the rule",
    "evaluation": { … } // The evaluation logic of the rule. More details below.
}
```

### Guidelines for rule metadata
| Property Name | Description | Is required for contributing<br/>a built-in rule | Is required<br/>in schema | Default Value |
|---|---|---|---|---|
| id | The `id` should look like `TA-NNNNNN`, with `NNNNNN` being the next unused number according to the [rule ids already defined](https://github.com/Azure/template-analyzer/blob/main/docs/built-in-bpa-rules.md). | yes | yes | - |
| name | A human-readable identifier, more details [here](https://github.com/microsoft/sarif-tutorials/blob/main/docs/Authoring-rule-metadata-and-result-messages.md#human-readable-identifier). | yes | yes | - |
| shortDescription | Brief description of what the rule is evaluating, more details [here](https://docs.oasis-open.org/sarif/sarif/v2.0/csprd02/sarif-v2.0-csprd02.html#_Toc10127743). | yes | yes | - |
| fullDescription | Detailed description of what the rule is evaluating, more details [here](https://docs.oasis-open.org/sarif/sarif/v2.0/csprd02/sarif-v2.0-csprd02.html#_Toc10127744). | yes | yes | - |
| recommendation | The `recommendation` should provide clear but concise guidance on how to modify a template if the rule fails.<br/>If some details are somewhat complex, or the rule takes a bit more to understand, add those details to a guide accessible at the URI in `helpUri`. | yes | no | none |
| helpUri | The `helpUri` is optional, but it is good practice to include.  For built-in rules, this will point to a guide in the GitHub repository. | yes | no | none |
| severity | The `severity` is optional. If no severity is provided, it defaults to a severity of 2. | yes | no | 2 |

## Evaluation Object
The `Evaluation` is comprised of the following basic properties.
```javascript
{
    "path": "JSON path to property to evaluate in template",
    "resourceType": "(optional) The Azure resource type this evaluation applies to",
    "where": {
        // Evaluation Object
    }
    "<operator>": // One of several kinds of operators, defined below
}
```

Evaluation of the templates is performed on the JSON representation of the template. Therefore, `Evaluation`s operate on the JSON properties of the template.  Specifying the template property is done by specifying a JSON path for the `path` key.  This path can contain wildcards ('\*') to select multiple paths to evaluate - see [Wildcard Behavior](#wildcard-behavior) for details.

Since most rules apply only to specific types of Azure resources, the `resourceType` property gives rule authors a shorthand to only evaluate those types of resources.  If `resourceType` is specified, the path specified in `path` becomes relative to the resource selected in the template.

When `resourceType` is specified, it must be the fully-qualified type name (for example, *Microsoft.Sql/servers/auditingSettings*, instead of simply *auditingSettings* as might be specified in a child resource of *Microsoft.Sql/servers*).

When looking for the specified resource type, the Template BPA will look for the "resources" array property at the current [scopes](#scopes), and if found, compare the "type" property of each resource against the string specified for *resourceType*.  The search will also include looking at child resources - i.e. a "resources" array property defined within a resource.  This will occur if a type-parent of the specified *resourceType* is found in the resources (e.g. if searching for type *Microsoft.Sql/servers/auditingSettings*, resources defined within a resource of type *Microsoft.Sql/servers* will also be searched).

Documentation on `where` is provided below in [Where Conditions](#where-conditions).

## Operators
There are two kinds of operators: [value operators](#value-operators) and [structured operators](#structured-operators).  Value operators evaluate a single value, whereas structured operators are used to nest and combine multiple `Evaluation`s, each containing their own operator.

### Value Operators
These operators evaluate a specific JSON property in the template.  All operators are valid properties in the `Evaluation`, but only one operator can be present in the top level of the `Evaluation`.  If multiple operators are necessary, a structured operator can be used to combine or nest the operators. Each operator must be accompanied by a `path` in the `Evaluation`. The type of value each operator expects is defined with each operator. Most types are self-descriptive; for the `date` type, the following [ISO 8601](https://www.iso.org/iso-8601-date-and-time-format.html) formats are currently accepted:

* yyyy-MM-dd
* yyyy-MM-ddThh:mm:ssK
* yyyy-MM-ddThh:mmK
* yyyy-MM-dd hh:mm:ssK

More information on the format identifiers can be found [here](https://docs.microsoft.com/dotnet/standard/base-types/custom-date-and-time-format-strings).

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
        },
        "customOutput": {
            "type": "string",
            "value": "A custom output string"
        }
    }
}
```

#### **Exists**
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

#### **HasValue**
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

#### **Equals**
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

#### **NotEquals**
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

#### **Less**
*Type: number (integer, float), date*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is less than the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "less": 1 // Evaluates to `false` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is not less than 1.
}
```

#### **LessOrEquals**
*Type: number (integer, float), date*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is less than or equal to the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "lessOrEquals": 1 // Evaluates to `true` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is equal to 1.
}
```

#### **Greater**
*Type: number (integer, float), date*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is greater than the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "greater": 0 // Evaluates to `true` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is greater than 0.
}
```

#### **GreaterOrEquals**
*Type: number (integer, float), date*

Compares the template value of the `path` against the value specified in the rule.  Evaluates to `true` if the template value is greater than or equal to the value in the template; `false` otherwise.

Example:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "outputs.numberOfResourcesDeployed.value",
    "greaterOrEquals": 2 // Evaluates to `false` because the value of the path "outputs.numberOfResourcesDeployed.value" (1) is not greater than or equal to 1.
}
```

#### **Regex**
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

#### **In**
*Type: mixed-type array of basic JSON values (integer, float, string, bool, null)*

Evaluates the value of the `path` in the template using the `equals` operator for each value specified in the array.  If any results in `true`, `in` will evaluate to true; `false` otherwise.

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
These operators build up a structure of child `Evaluation`s, and therefore contain additional operators inside them.  These operators are not required to include a `path`.  If `resourceType` or `path` are specified, that becomes the scope for all `Evaluation`s nested inside the operator.  More information on [Scopes](#scopes) can be found below.

#### **AnyOf**
*Type: array of [`Evaluation`s](#evaluation-object)*

Performs a logical 'or' operation on the array of `Evaluation`s.  Evaluates to `true` if the result of any `Evaluation` in the array is `true`; evaluates to `false` otherwise.

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

#### **AllOf**
*Type: array of [`Evaluation`s](#evaluation-object)*

Performs a logical 'and' operation on the array of `Evaluation`s.  Evaluates to `true` if the result of all `Evaluation`s in the array is `true`; evaluates to `false` otherwise.

Example:
```javascript
{
    "allOf": [
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

#### **Not**
*Type: [`Evaluation`](#evaluation-object)*

Performs a logical 'not' operation on the `Evaluation`.  Evaluates to `true` if the result of the `Evaluation` it contains is `false`; evaluates to `false` otherwise.

Example:
```javascript
{
    "not": {
        "resourceType": "Microsoft.Compute/virtualMachines",
        "path": "properties.osProfile.adminUsername",
        "regex": "admin" // Evaluates to `false`
    } // Evaluates to `true` because it's the logical 'not' of the sub-evaluation that is `false`
}
```

#### **Evaluate [[*]](#note)**
*Type: [`Evaluation`](#evaluation-object)*

**NOTE: `evaluate` is not yet supported.  As a workaround, replace it with an `allOf` or `anyOf` operator containing a single `Evaluation`. See the examples in [Where Conditions](#where-conditions) for what this would look like.**

Wraps a single `Evaluation`.  The result of the operator is exactly the result of the `Evaluation` it contains.

This operator is most commonly useful in combination with a [`where` condition](#where-conditions), where `resourceType` or `path` may need to be [scoped down](#scopes) multiple times.

Example:
```javascript
{
    "evaluate": {
        "resourceType": "Microsoft.Compute/virtualMachines",
        "path": "properties.osProfile.adminPassword",
        "hasValue": false // Evaluates to `true`
    } // Evaluates to the same as the inner evaluation (`true`)
}
```

## Scopes
Each `Evaluation` has a path scope which is inherited by child `Evaluation`s.  If a `path` and/or `resourceType` is specified in a Structured `Evaluation`, the `path` and `resourceType` of each child `Evaluation` start at the path determined in the parent.  Therefore, each `path` continues from the `path` specified in the parent.
For example, here's a simple illustration:
```javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "allOf": [
        {
            "path": "properties.osProfile.adminPassword", // Continues from resource selected in parent scope
            "hasValue": false
        }
    ]
}
```
 
The full path used by the 'hasValue' Evaluator would be *resources[\*].properties.osProfile.adminPassword* (limited to resources where *type* equals "Microsoft.Compute/virtualMachines").
 
First, `resourceType` is used to select resources within the *resources[]* array.  Then, only those resources with the given type are considered.  Further, the `path` specified with `hasValue` continues from the path in the parent scope, appending *.properties.osProfile.adminPassword* to the resources selection.

## Where Conditions
Rule authors may wish to define a rule that is dependent on other properties.  For example, there may be desire for a rule of the form: "If a property in a resource equals some value, then assert another property is a certain value."

Conditions like this can be defined using the `where` property, which has a value of type `Evaluation` (similar to [Structured Operators](#structured-operators)).

In the `Evaluation` in which `where` is defined, `resourceType` and `path` are evaluated first, and then the resulting [scopes](#scopes) are evaluated by `where`.  Since `where` is an `Evaluation`, the scope can be further narrowed by specifying `resourceType` or `path` inside it.

Multiple [scopes](#scopes) may be evaluated by `where` as a result of:
- multiple resources matching the `resourceType` specification
- [wildcards](#wildcard-behavior) being present in `path` and matching multiple paths in the template JSON

For each [scope](#scopes) evaluated by `where`, only the scopes in which `where` evaluates to `true` are evaluated by the operator that is a sibling of `where`; if the `where` evaluates to `false` for a given scope, evaluation of the operator will be skipped for that scope.

Examples:
``` javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "where": {
        "path": "apiVersion",
        "regex": "^2019-.*" // "Microsoft.Compute/virtualMachines" resources where the value of "apiVersion" matches this regex...
    },
    "allOf": [
        {
            "path": "properties.osProfile.computerName",
            "hasValue": true // ...must have a value for "properties.osProfile.computerName".
        }
    ]
}
```
In the simple example above, the `allOf` operator would be skipped, because there is no resource of type "Microsoft.Compute/virtualMachines" where apiVersion starts with 2019.  The entire example would therefore not return any result.

``` javascript
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "where": {
        "path": "name",
        "equals": "myVmResource" // "Microsoft.Compute/virtualMachines" resources where the value of "name" is "myVmResource"...
    },
    "allOf": [
        {
            "path": "properties.osProfile.computerName",
            "hasValue": true // ...must have a value for "properties.osProfile.computerName".
        }
    ]
}
```
In contrast to the first example, the `allOf` operator in the example above would be evaluated, because the resource of type "Microsoft.Compute/virtualMachines" defines its "name" property to be "myVmResource".

**NOTE:** In both examples above, `"path": "properties.osProfile.computerName"` is specified *inside* the `allOf` operators.  This is important because of how [scopes](#scopes) are determined.  If it was instead specified outside the operator (as a sibling to `where`), it would narrow the **outer** scope to that path.  That path would then be passed into `where`, resulting in `"path": "apiVersion"` and `"path": "name"` (inside `where` in the examples) being appended to *properties.osProfile.computerName* in the outer scope, which is not the intent.

## Wildcard Behavior
The `path` in an `Evaluation` can specify the '\*' character as a wildcard.  '\*' can be used to match any full property name or as the index into an array (selecting all elements of the array).  When a wildcard is used, zero or more paths in the template will be found that match `path`.  If zero paths are found, the operator in the `Evaluation` is skipped, as there is nothing to evaluate.  If two or more paths are found, the operator evaluates each path individually and treats each path as its own result; then, if the operator evaluating the path(s) is contained within an `allOf` or `anyOf`, the results will be combined according to those operators - otherwise, they will be reported individually.

When using a wildcard for a property name, '\*' must replace the entire name of a property (such as *property.\** or *property.\*.otherProperty*), being the only character between the periods.  Wildcards for partial property names (e.g. *property.\*id*) are **not** supported.  When using a wildcard as an index into an array (such as *property[\*]*), '\*' must be the only character between the '[]' characters.

Examples:

``` js
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.osProfile.*" // Returns all child properties of osProfile:
        // resources[0].properties.osProfile.computerName
        // resources[0].properties.osProfile.adminUsername
        // resources[0].properties.osProfile.adminPassword
}
```

``` js
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "properties.networkProfile.networkInterfaces[*]" // Returns all elements in networkInterfaces array (only one element is defined in the array):
        // resources[0].properties.networkProfile.networkInterfaces[0]
}
```

``` js
{
    "path": "resources[*]" // Returns all resources (only one resource is defined):
        // resources[0]
}
```

``` js
{
    "path": "outputs.*" // Returns all outputs:
        // outputs.numberOfResourcesDeployed
        // outputs.customOutput
}
```

``` js
{
    "resourceType": "Microsoft.Compute/virtualMachines",
    "path": "tags.*" // Returns all child properties of 'tags' - no paths returned, as no tags are defined in the virtual machine resource
}
```
