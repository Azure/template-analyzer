@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Latest TLS version is used
@description('API App with latest TLS version enabled (positive case)')
resource apiAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'api-app-positive'
  location: location
  kind: 'api'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2' // Latest TLS version is set
    }
  }
}

// Positive Case: Latest TLS version is used in site config
@description('API App with latest TLS version enabled in site config (positive case)')
resource apiAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
 parent: apiAppPositive
  name: 'web'
  properties: {
    minTlsVersion: '1.2' // Latest TLS version is set
  }
}

// Negative Case: TLS version is not latest
@description('API App without latest TLS version enabled (negative case)')
resource apiAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'api-app-negative'
  location: location
  kind: 'api'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0' // Older TLS version is set (violation)
    }
  }
}

// Negative Case: TLS version is not latest in site config
@description('API App without latest TLS version enabled in site config (negative case)')
resource apiAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
 parent: apiAppNegative
  name: 'web'
  properties: {
    minTlsVersion: '1.0' // Older TLS version is set (violation)
  }
}

// Negative Case: TLS version is not set
@description('API App without TLS version enabled (negative case)')
resource apiAppNotDefinedNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'api-app-not-defined-negative'
  location: location
  kind: 'api'
  properties: {
    siteConfig: {
      // TLS version is not set (violation)
    }
  }
}

// Negative Case: TLS version is not set in site config
@description('API App without TLS version enabled in site config (negative case)')
resource apiAppNotDefinedNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
 parent: apiAppNotDefinedNegative
  name: 'web'
  properties: {
      // TLS version is not set (violation)
  }
}
