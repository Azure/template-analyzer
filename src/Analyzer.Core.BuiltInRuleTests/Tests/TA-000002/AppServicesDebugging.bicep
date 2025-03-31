@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Remote debugging disabled
@description('API App with remote debugging disabled (positive case)')
resource apiAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'api-app-positive'
  location: location
  kind: 'api'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: false // Remote debugging is turned off
    }
  }
}

// Positive Case: Remote debugging disabled in site config
@description('API App with remote debugging disabled in site config (positive case)')
resource apiAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: apiAppPositive
  name: 'web'
  properties: {
    remoteDebuggingEnabled: false // Remote debugging is turned off
  }
}

// Negative Case: Remote debugging enabled
@description('API App with remote debugging enabled (negative case)')
resource apiAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'api-app-negative'
  location: location
  kind: 'api'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
    }
  }
}

// Negative Case: Remote debugging enabled in site config
@description('API App with remote debugging enabled in site config (negative case)')
resource apiAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: apiAppNegative
  name: 'web'
  properties: {
    remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
  }
}
