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