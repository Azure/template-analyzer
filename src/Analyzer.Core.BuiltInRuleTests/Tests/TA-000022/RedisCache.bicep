@description('Location of all resources')
param location string = resourceGroup().location

resource withoutSpecifyingProperties 'Microsoft.Cache/redis@2020-06-01' = {
  name: 'withoutSpecifyingProperties'
  location: location
  properties: {
  }
}

resource withAnEnabledNonSslPort 'Microsoft.Cache/redis@2020-06-01' = {
  name: 'withAnEnabledNonSslPort'
  location: location
  properties: {
    enableNonSslPort: true
  }
}

resource withADisabledNonSslPort 'Microsoft.Cache/redis@2020-06-01' = {
  name: 'withADisabledNonSslPort'
  location: location
  properties: {
    enableNonSslPort: false
  }
}