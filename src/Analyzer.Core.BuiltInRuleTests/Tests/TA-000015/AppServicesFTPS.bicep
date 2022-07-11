@description('Location for all resources.')
param location string = resourceGroup().location

resource linuxKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'linux'
  name: 'linuxKind'
  location: location
  properties: {
  }
}

resource withoutSpecifyingProperties 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'withoutSpecifyingProperties'
  location: location
  properties: {
  }
}

resource undesiredFtpsState 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'undesiredFtpsState'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'undesiredValue'
    }
  }
}

resource ftpsOnly 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ftpsOnly'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'FtpsOnly'
    }
  }
}

resource ftpsStateDisabled 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ftpsStateDisabled'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'Disabled'
    }
  }
}

resource withoutSpecifyingPropertiesForSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'withoutSpecifyingPropertiesForSitesConfig'
  location: location
  properties: {
  }
}

resource sitesConfig_desiredFtpsState 'Microsoft.Web/sites/config@2019-08-01' = {
  kind: 'api'
  name: 'sitesConfig/desiredFtpsState'
  location: location
  properties: {
    ftpsState: 'FtpsOnly'
  }
  dependsOn: [
    withoutSpecifyingPropertiesForSitesConfig
  ]
}

resource misingKindPropertyWithUndesiredFtpsState 'Microsoft.Web/sites@2019-08-01' = {
  name: 'misingKindPropertyWithUndesiredFtpsState'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'undesiredValue'
    }
  }
}

resource misingKindPropertyWithDesiredFtpsState 'Microsoft.Web/sites@2019-08-01' = {
  name: 'misingKindPropertyWithDesiredFtpsState'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'FtpsOnly'
    }
  }
}

resource misingKindPropertyAndFtpsState 'Microsoft.Web/sites@2019-08-01' = {
  name: 'misingKindPropertyAndFtpsState'
  location: location
  properties: {
  }
}

resource sitesConfigDependingOnAResourceWithoutKindProperty_desiredFtpsState 'Microsoft.Web/sites/config@2019-08-01' = {
  name: 'sitesConfigDependingOnAResourceWithoutKindProperty/desiredFtpsState'
  location: location
  properties: {
    ftpsState: 'FtpsOnly'
  }
  dependsOn: [
    misingKindPropertyAndFtpsState
  ]
}