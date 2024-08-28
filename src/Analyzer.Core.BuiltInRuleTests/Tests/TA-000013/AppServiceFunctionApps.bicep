@description('Location for all resources.')
param location string = resourceGroup().location

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'managedIdentity'
  location: location
}
resource missingIdentity 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'missingIdentity'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
}

resource systemManagedIdentity 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'systemManagedIdentity'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
  identity: {
    type: 'SystemAssigned'
  }
}

resource userManagedIdentity 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'userManagedIdentity'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
}

resource systemAndUserManagedIdentity 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'systemAndUserManagedIdentity'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
  identity: {
    type: 'SystemAssigned,UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
}

resource systemAndUserManagedWithSpaceIdentity 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'systemAndUserManagedWithSpaceIdentity'
  location: location
  properties: {
    siteConfig: {
      detailedErrorLoggingEnabled: false
      httpLoggingEnabled: false
      requestTracingEnabled: false
    }
  }
  identity: {
    type: 'SystemAssigned, UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
}
