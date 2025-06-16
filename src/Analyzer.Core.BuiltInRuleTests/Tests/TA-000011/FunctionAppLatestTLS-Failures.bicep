resource functionAppTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppTls10'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0'
      }
  }
}

resource functionAppNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppNoTls'
  properties: {
    siteConfig: {
      }
  }
}

resource functionAppSeparateConfigTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppSeparateConfigTls10'
}

resource functionAppConfigTls10 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionAppSeparateConfigTls10
  name: 'web'
  properties: {
    minTlsVersion: '1.0'
  }
}

resource functionAppSeparateConfigNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppSeparateConfigNoTls'
}

resource functionAppConfigNoTls 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionAppSeparateConfigNoTls
  name: 'web'
  properties: {
  }
}
