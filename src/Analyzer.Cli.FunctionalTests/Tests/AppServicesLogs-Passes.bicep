@description('Location for all resources.')
param location string = resourceGroup().location

@description('Specifies managed identity name')
param managedIdentityName string

resource functionAppKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'functionAppKind'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      detailedErrorLoggingEnabled: false
      ftpsState: 'Disabled'
      httpLoggingEnabled: false
      minTlsVersion: '1.2'
      requestTracingEnabled: false
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityName_resource.id}': {
      }
    }
  }
}

resource managedIdentityName_resource 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: managedIdentityName
  location: location
}