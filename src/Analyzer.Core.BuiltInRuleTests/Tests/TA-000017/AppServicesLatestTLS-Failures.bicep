resource webAppTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppTls10'
  properties: {
    siteConfig: {
      minTlsVersion: '1.0'
      }
  }
}

resource webAppNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppTls'
  properties: {
    siteConfig: {
      }
  }
}

resource webAppSeparateConfigTls10 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppSeparateConfigTls10'
}

resource webAppConfigTls10 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: webAppSeparateConfigTls10
  name: 'web'
  properties: {
    minTlsVersion: '1.0'
  }
}

resource webAppSeparateConfigNoTls 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppSeparateConfigNoTls'
}

resource webAppConfigNoTls 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: webAppSeparateConfigNoTls
  name: 'web'
  properties: {
  }
}
