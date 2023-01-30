@description('Location for all resources.')
param location string = resourceGroup().location

resource functionAppKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'functionAppKind'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource linuxKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'linux'
  name: 'linuxKind'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource withLinuxKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app,linux'
  name: 'withLinuxKind'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource diagLogsDisabledInSiteConfigPropertyButEnabledInDependentConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'diagLogsDisabledInSiteConfigPropertyButEnabledInDependentConfig'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource diagLogsEnabledInSiteConfigProperty 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'diagLogsEnabledInSiteConfigProperty'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: true
      httpLoggingEnabled: true
      requestTracingEnabled: true
    }
  }
}

resource sitesConfig_diagLogsEnabled 'Microsoft.Web/sites/config@2019-08-01' = {
  kind: 'app'
  name: 'sitesConfig/diagLogsEnabled'
  location: location
  properties: {
    detailedErrorLoggingEnabled: true
    httpLoggingEnabled: true
    requestTracingEnabled: true
  }
  dependsOn: [
    diagLogsDisabledInSiteConfigPropertyButEnabledInDependentConfig
  ]
}

resource sitesConfigDependingOnLinuxKind_diagLogsDisabled 'Microsoft.Web/sites/config@2019-08-01' = {
  kind: 'app'
  name: 'sitesConfigDependingOnLinuxKind/diagLogsDisabled'
  location: location
  properties: {
    detailedErrorLoggingEnabled: false
    httpLoggingEnabled: false
    requestTracingEnabled: false
  }
  dependsOn: [
    linuxKind
  ]
}

resource withConfigAsChildResource 'Microsoft.Web/sites@2019-08-01' = {
  name: 'withConfigAsChildResource'
  kind: 'app'
  properties: {
  }
}

resource withConfigAsChildResource_web 'Microsoft.Web/sites/config@2019-08-01' = {
  parent: withConfigAsChildResource
  name: 'web'
  properties: {
    detailedErrorLoggingEnabled: true
    httpLoggingEnabled: true
    requestTracingEnabled: true
  }
}