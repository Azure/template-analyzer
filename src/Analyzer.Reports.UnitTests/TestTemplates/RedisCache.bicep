@description('Location of all resources')
param location string = resourceGroup().location

resource withAnEnabledNonSslPort 'Microsoft.Cache/redis@2020-06-01' = {
  name: 'withAnEnabledNonSslPort'
  location: location
  properties: {
    enableNonSslPort: true
  }
}