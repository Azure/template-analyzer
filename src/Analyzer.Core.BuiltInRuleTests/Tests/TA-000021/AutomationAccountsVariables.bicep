@description('Location for all resources.')
param location string = resourceGroup().location

resource automationAccounts_isEncrypted 'Microsoft.Automation/automationAccounts/variables@2021-04-01' = {
  name: 'automationAccounts/isEncrypted'
  location: location
  properties: {
    isEncrypted: true
  }
}

resource automationAccounts_isNotEncrypted 'Microsoft.Automation/automationAccounts/variables@2021-04-01' = {
  name: 'automationAccounts/isNotEncrypted'
  location: location
  properties: {
    isEncrypted: false
  }
}

resource automationAccountsWithADependant 'Microsoft.Automation/automationAccounts@2021-04-01' = {
  name: 'automationAccountsWithADependant'
  location: location
}

resource automationAccounts_isEncryptedNotDefined 'Microsoft.Automation/automationAccounts/variables@2021-04-01' = {
  name: 'automationAccounts/isEncryptedNotDefined'
  location: location
  properties: {
  }
  dependsOn: [
    automationAccountsWithADependant
  ]
}

resource justAutomationAccounts 'Microsoft.Automation/automationAccounts@2021-04-01' = {
  name: 'justAutomationAccounts'
  location: location
}

resource justAutomationAccounts_isNotEncrypted 'Microsoft.Automation/automationAccounts/variables@2021-04-01' = {
  parent: justAutomationAccounts
  name: 'isNotEncrypted'
  properties: {
    isEncrypted: false
  }
}