param storageAccountName string

resource ASQLServerName 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'ASQLServerName'
  properties: {
    kind: 'v12.0'
  }
}

resource ASQLServerName_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: ASQLServerName
  name: 'default'
  properties: {
    state: 'Enabled'
    isAzureMonitorTargetEnabled: true
    storageEndpoint: ''
    storageAccountAccessKey: listKeys(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').keys[0].value
    retentionDays: 90
    storageAccountSubscriptionId: subscription().subscriptionId
    isStorageSecondaryKeyInUse: false
  }
}

resource With90RetentionDays 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'With90RetentionDays'
  properties: {
    kind: 'v12.0'
  }
}

resource With90RetentionDays_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: With90RetentionDays
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 90
  }
}

resource WithMoreThan90RetentionDays 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'WithMoreThan90RetentionDays'
  properties: {
    kind: 'v12.0'
  }
}

resource WithMoreThan90RetentionDays_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: WithMoreThan90RetentionDays
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 95
  }
}

resource With0RetentionDays 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'With0RetentionDays'
  properties: {
    kind: 'v12.0'
  }
}

resource With0RetentionDays_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: With0RetentionDays
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 0
  }
}

resource WithEmptyStorageEndpointAndAzureMonitorTargetEnabled 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'WithEmptyStorageEndpointAndAzureMonitorTargetEnabled'
  properties: {
    kind: 'v12.0'
  }
}

resource WithEmptyStorageEndpointAndAzureMonitorTargetEnabled_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: WithEmptyStorageEndpointAndAzureMonitorTargetEnabled
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: true
    storageEndpoint: ''
    retentionDays: 45
  }
}

resource WithNoStorageEndpointAndAzureMonitorTargetEnabled 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'WithNoStorageEndpointAndAzureMonitorTargetEnabled'
  properties: {
    kind: 'v12.0'
  }
}

resource WithNoStorageEndpointAndAzureMonitorTargetEnabled_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: WithNoStorageEndpointAndAzureMonitorTargetEnabled
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: true
    retentionDays: 45
  }
}

resource OfAnalyticsKind 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'OfAnalyticsKind'
  properties: {
    kind: 'analytics'
  }
}

resource OfAnalyticsKind_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: OfAnalyticsKind
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource WithNonDefaultAuditingSettings 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'WithNonDefaultAuditingSettings'
  properties: {
    kind: 'v12.0'
  }
}

resource WithNonDefaultAuditingSettings_anotherName 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: WithNonDefaultAuditingSettings
  name: 'anotherName'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource EndsInDefaultButNotOnlyDefault 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'EndsInDefaultButNotOnlyDefault'
  properties: {
    kind: 'v12.0'
  }
}

resource EndsInDefaultButNotOnlyDefault_adefault 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: EndsInDefaultButNotOnlyDefault
  name: 'adefault'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource StartsWithDefaultButNotOnlyDefault 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'StartsWithDefaultButNotOnlyDefault'
  properties: {
    kind: 'v12.0'
  }
}

resource StartsWithDefaultButNotOnlyDefault_defaulta 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: StartsWithDefaultButNotOnlyDefault
  name: 'defaulta'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource SecondSegmentIsNotOnlyDefault 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'SecondSegmentIsNotOnlyDefault'
  properties: {
    kind: 'v12.0'
  }
}

resource SecondSegmentIsNotOnlyDefault_defaultPlusMore 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: SecondSegmentIsNotOnlyDefault
  name: 'defaultPlusMore'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 45
  }
}

resource ASqlServerWithoutChildResources 'Microsoft.Sql/servers@2020-02-02-preview' = {
  name: 'ASqlServerWithoutChildResources'
  properties: {
    kind: 'v12.0'
  }
}

resource ASqlServerWithoutChildResources_default 'Microsoft.Sql/servers/auditingSettings@2020-02-02-preview' = {
  parent: ASqlServerWithoutChildResources
  name: 'default'
  properties: {
    isAzureMonitorTargetEnabled: false
    storageEndpoint: reference(resourceId('Microsoft.Storage/storageAccounts', storageAccountName), '2019-06-01').PrimaryEndpoints.Blob
    retentionDays: 95
  }
}