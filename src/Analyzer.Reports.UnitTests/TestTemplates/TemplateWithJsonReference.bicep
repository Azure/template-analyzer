@description('Storage account name')
param storageAccountName string = 'testStorageAccountName'

module nestedTemplate './SQLServerAuditingSettings.json' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    storageAccountName: storageAccountName
  }
}

// test deduping of results, will yield duplicate evaluations
module nestedTemplate2 './SQLServerAuditingSettings.json' = {
  name: 'nestedTemplate2'
  scope: resourceGroup('my-rg')
  params: {
    storageAccountName: storageAccountName
  }
}
