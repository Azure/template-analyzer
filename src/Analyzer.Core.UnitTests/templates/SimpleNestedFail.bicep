param location string = resourceGroup().location
param ftpsState string = 'FtpsOnly'

var ftpsState_var = 'FtpsOnly'

module nestedTemplate './SimpleNestedFailModule.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    variables_ftpsState: ftpsState_var
    location: location
    ftpsState: ftpsState
  }
}