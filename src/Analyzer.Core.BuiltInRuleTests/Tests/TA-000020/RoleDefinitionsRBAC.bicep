// Positive Case: Built-in RBAC role used
@description('Role Definition with built-in RBAC role (positive case)')
resource builtInRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: guid(subscription().id, 'Owner')
  properties: {
    roleName: 'Owner'
    type: 'BuiltInRole' // Built-in RBAC role
  }
}

// Negative Case: Custom RBAC role used
@description('Role Definition with custom RBAC role (negative case)')
resource customRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: guid(subscription().id, 'CustomRole')
  properties: {
    roleName: 'CustomRole'
    type: 'CustomRole' // Custom RBAC role (violation)
  }
}
