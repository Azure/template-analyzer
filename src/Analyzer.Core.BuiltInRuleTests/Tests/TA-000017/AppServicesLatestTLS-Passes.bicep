resource webAppTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppTls12'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2'
      }
  }
}

resource webAppTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppTls13'
  properties: {
    siteConfig: {
      minTlsVersion: '1.3'
      }
  }
}

resource webAppSeparateConfigTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppSeparateConfigTls12'
}

resource webAppConfigTls12 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: webAppSeparateConfigTls12
  name: 'web'
  properties: {
    minTlsVersion: '1.2'
  }
}

resource webAppSeparateConfigTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'app'
  name: 'webAppSeparateConfigTls13'
}

resource webAppConfigTls13 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: webAppSeparateConfigTls13
  name: 'web'
  properties: {
    minTlsVersion: '1.3'
  }
}
