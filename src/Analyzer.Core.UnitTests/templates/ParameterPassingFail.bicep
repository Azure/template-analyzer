@description('The location where the resources will be deployed.')
param location string = 'location'

@description('The name of the keyvault that contains the secret.')
param ftpsstate string = 'FtpsOnly'

module dynamicSecret './ParameterPassingFailModule.bicep' = {
  name: 'dynamicSecret'
  params: {
    location: location
    ftpsstate: ftpsstate
  }
}