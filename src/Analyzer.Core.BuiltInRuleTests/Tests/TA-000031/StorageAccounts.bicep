resource aClassicStorageAccount 'Microsoft.ClassicStorage/storageAccounts@2021-08-01' = {
  name: 'aClassicStorageAccount'
  properties: {
  }
}

resource anotherTypeOfStorageAccount 'Microsoft.Storage/storageAccounts@2022-03-01' = {
  name: 'anotherTypeOfStorageAccount'
  properties: {
  }
}