param location string

module m1 '../Module2.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

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