@description('Location for all resources.')
param location string = resourceGroup().location

module nestedTemplate './SimpleNestedFailModule.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}