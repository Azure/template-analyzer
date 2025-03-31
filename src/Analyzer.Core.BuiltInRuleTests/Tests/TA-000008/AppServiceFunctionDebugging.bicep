@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Remote debugging disabled for function app
@description('Function App with remote debugging disabled (positive case)')
resource functionAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-positive'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: false // Remote debugging is turned off
    }
  }
}

// Positive Case: Remote debugging disabled in site config for function app
@description('Function App with remote debugging disabled in site config (positive case)')
resource functionAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppPositive
  name: 'web'
  properties: {
    remoteDebuggingEnabled: false // Remote debugging is turned off
  }
}

// Negative Case: Remote debugging enabled for function app
@description('Function App with remote debugging enabled (negative case)')
resource functionAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-negative'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
    }
  }
}

// Negative Case: Remote debugging enabled in site config for function app
@description('Function App with remote debugging enabled in site config (negative case)')
resource functionAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppNegative
  name: 'web'
  properties: {
    remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
  }
}
