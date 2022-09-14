@description('Location of all resources')
param location string = resourceGroup().location

@description('Storage account name')
param storageAccountName string = 'testStorageAccountName'

module nestedTemplate './RedisCache.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplate2 './SQLServerAuditingSettings.bicep' = {
  name: 'nestedTemplate2'
  scope: resourceGroup('my-rg')
  params: {
    storageAccountName: storageAccountName
  }
}
