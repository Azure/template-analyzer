@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Latest TLS version is used
@description('Function App with latest TLS version enabled (positive case)')
resource functionAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-positive'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2' // Latest TLS version is set
    }
  }
}

// Positive Case: Latest TLS version is used in site config
@description('Function App with latest TLS version enabled in site config (positive case)')
resource functionAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppPositive
  name: 'web'
  properties: {
    minTlsVersion: '1.2' // Latest TLS version is set
  }
}

// Negative Case: TLS version is not latest
@description('Function App without latest TLS version enabled (negative case)')
resource functionAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-negative'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0' // Older TLS version is set (violation)
    }
  }
}

// Negative Case: TLS version is not latest in site config
@description('Function App without latest TLS version enabled in site config (negative case)')
resource functionAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppNegative
  name: 'web'
  properties: {
    minTlsVersion: '1.0' // Older TLS version is set (violation)
  }
}

// Negative Case: TLS version is not set
@description('Function App without TLS version enabled (negative case)')
resource functionAppNotDefinedNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-not-defined-negative'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      // TLS version is not set (violation)
    }
  }
}

// Negative Case: TLS version is not set in site config
@description('Function App without TLS version enabled in site config (negative case)')
resource functionAppNotDefinedNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppNotDefinedNegative
  name: 'web'
  properties: {
      // TLS version is not set (violation)
  }
}
