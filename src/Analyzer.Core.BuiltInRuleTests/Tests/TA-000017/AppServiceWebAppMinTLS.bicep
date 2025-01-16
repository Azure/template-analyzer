@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: Latest TLS version is used
@description('Web App with latest TLS version enabled (positive case)')
resource webAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-app-positive'
  location: location
  kind: 'app'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2' // Latest TLS version is set
    }
  }
}

// Positive Case: Latest TLS version is used in site config
@description('Web App with latest TLS version enabled in site config (positive case)')
resource webAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webAppPositive
  name: 'web'
  properties: {
    minTlsVersion: '1.2' // Latest TLS version is set
  }
}

// Negative Case: TLS version is not latest
@description('Web App without latest TLS version enabled (negative case)')
resource webAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-app-negative'
  location: location
  kind: 'app'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0' // Older TLS version is set (violation)
    }
  }
}

// Negative Case: TLS version is not latest in site config
@description('Web App without latest TLS version enabled in site config (negative case)')
resource webAppNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webAppNegative
  name: 'web'
  properties: {
    minTlsVersion: '1.0' // Older TLS version is set (violation)
  }
}

// Negative Case: TLS version is not set
@description('Web App without TLS version enabled (negative case)')
resource webAppNotDefinedNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'web-app-not-defined-negative'
  location: location
  kind: 'app'
  properties: {
    siteConfig: {
      // TLS version is not set (violation)
    }
  }
}

// Negative Case: TLS version is not set in site config
@description('Web App without TLS version enabled in site config (negative case)')
resource webAppNotDefinedNegativeConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: webAppNotDefinedNegative
  name: 'web'
  properties: {
      // TLS version is not set (violation)
  }
}
