@description('Location for all resources.')
param location string = resourceGroup().location

resource diagLogsDisabledInSiteConfigProperty 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'diagLogsDisabledInSiteConfigProperty'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource diagLogsDisabledInSiteConfigPropertyAndDependentConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'diagLogsDisabledInSiteConfigPropertyAndDependentConfig'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource sitesConfig_diagLogsDisabled 'Microsoft.Web/sites/config@2019-08-01' = {
  kind: 'app'
  name: 'sitesConfig/diagLogsDisabled'
  location: location
  properties: {
    detailedErrorLoggingEnabled: false
    httpLoggingEnabled: false
    requestTracingEnabled: false
  }
  dependsOn: [
    diagLogsDisabledInSiteConfigPropertyAndDependentConfig
  ]
}

resource aPropertyRequiredAsEnabledIsDisabled 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'aPropertyRequiredAsEnabledIsDisabled'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: true
      httpLoggingEnabled: false
      requestTracingEnabled: true
    }
  }
}

resource diagLogsMissingInSiteConfigProperty 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'diagLogsMissingInSiteConfigProperty'
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    httpsOnly: true
    siteConfig: {
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
    }
  }
}