@description('Location for all resources.')
param location string = 'testLocation'

module nestedTemplate './AppServicesLogs-Failures.json' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplate2 './AppServicesLogs-Failures.json' = {
  name: 'nestedTemplate2'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}
