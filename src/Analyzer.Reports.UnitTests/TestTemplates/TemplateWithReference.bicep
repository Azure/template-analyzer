@description('Storage account name')
param storageAccountName string = 'testStorageAccountName'

module nestedTemplate './SQLServerAuditingSettings.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    storageAccountName: storageAccountName
  }
}
