param variables_ftpsState string
param location string
param ftpsState string

resource linuxKind 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'linux'
  name: 'linuxKind'
  location: location
  properties: {
    siteConfig: {
      ftpsState: ftpsState
    }
  }
}

resource withoutSpecifyingProperties 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'withoutSpecifyingProperties'
  location: location
  properties: {
    siteConfig: {
      ftpsState: variables_ftpsState
    }
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