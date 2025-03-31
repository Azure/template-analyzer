@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Remote debugging disabled for web app
@description('Web App with remote debugging disabled (positive case)')
resource webAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-app-positive'
  location: location
  kind: 'app'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: false // Remote debugging is turned off
    }
  }
}

// Positive Case: Remote debugging disabled in site config for web app
@description('Web App with remote debugging disabled in site config (positive case)')
resource webAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webAppPositive
  name: 'web'
  properties: {
    remoteDebuggingEnabled: false // Remote debugging is turned off
  }
}

// Negative Case: Remote debugging enabled for web app
@description('Web App with remote debugging enabled (negative case)')
resource webAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-app-negative'
  location: location
  kind: 'app'
  properties: {
    siteConfig: {
      remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
    }
  }
}

// Negative Case: Remote debugging enabled in site config for web app
@description('Web App with remote debugging enabled in site config (negative case)')
resource webAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webAppNegative
  name: 'web'
  properties: {
    remoteDebuggingEnabled: true // Remote debugging is turned on (violation)
  }
}
