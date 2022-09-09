@description('Location for all resources.')
param location string = resourceGroup().location

module m1 './Module.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module m2 '../Module2.bicep' = {
  name: 'nestedTemplate2'
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