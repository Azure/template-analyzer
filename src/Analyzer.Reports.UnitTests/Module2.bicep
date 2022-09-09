param location string

resource r1 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'undesiredFtpsState'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'undesiredValue'
    }
  }
}

resource r2 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'undesiredFtpsState2'
  location: location
  properties: {
    siteConfig: {
      ftpsState: 'undesiredValue'
    }
  }
}