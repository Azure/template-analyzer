param storageAccountName string

resource FailsSQLRetentionDaysRule 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'FailsSQLRetentionDaysRule'
  properties: {
    kind: 'v12.0'
  }
}

resource FailsSQLRetentionDaysRule_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: FailsSQLRetentionDaysRule
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource FailsSQLRetentionDaysRuleWithFullResourceTypeAndName 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'FailsSQLRetentionDaysRuleWithFullResourceTypeAndName'
  properties: {
    kind: 'v12.0'
  }
}

resource FailsSQLRetentionDaysRuleWithFullResourceTypeAndName_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: FailsSQLRetentionDaysRuleWithFullResourceTypeAndName
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}