resource apiAppTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppTls10'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0'
      }
  }
}

resource apiAppNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppNoTls'
  properties: {
    siteConfig: {
      }
  }
}

resource apiAppSeparateConfigTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppSeparateConfigTls10'
}

resource apiAppConfigTls10 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: apiAppSeparateConfigTls10
  name: 'web'
  properties: {
    minTlsVersion: '1.0'
  }
}

resource apiAppSeparateConfigNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppSeparateConfigNoTls'
}

resource apiAppConfigNoTls 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: apiAppSeparateConfigNoTls
  name: 'web'
  properties: {
  }
}
