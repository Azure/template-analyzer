param innerResourceGroup string

@description('Location for all resources.')
param location string = resourceGroup().location

module nestedTemplate1 './DoubleNestedFailModule1.bicep' = {
  name: 'nestedTemplate1'
  scope: resourceGroup(innerResourceGroup)
  params: {
    location: location
    innerResourceGroup: innerResourceGroup
  }
}