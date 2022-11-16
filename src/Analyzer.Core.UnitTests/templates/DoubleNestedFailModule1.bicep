@description('Location for all resources.')
param location string
param innerResourceGroup string

resource withoutSpecifyingProperties 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'withoutSpecifyingProperties'
  location: location
  properties: {
  }
}

module nestedTemplate2 './DoubleNestedFailModule2.bicep' = {
  name: 'nestedTemplate2'
  scope: resourceGroup(innerResourceGroup)
  params: {
    location: location
  }
}