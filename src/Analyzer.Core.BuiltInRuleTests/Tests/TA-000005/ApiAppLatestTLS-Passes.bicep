resource apiAppTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppTls12'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2'
      }
  }
}

resource apiAppTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppTls13'
  properties: {
    siteConfig: {
      minTlsVersion: '1.3'
      }
  }
}

resource apiAppSeparateConfigTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppSeparateConfigTls12'
}

resource apiAppConfigTls12 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: apiAppSeparateConfigTls12
  name: 'web'
  properties: {
    minTlsVersion: '1.2'
  }
}

resource apiAppSeparateConfigTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'api'
  name: 'apiAppSeparateConfigTls13'
}

resource apiAppConfigTls13 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: apiAppSeparateConfigTls13
  name: 'web'
  properties: {
    minTlsVersion: '1.3'
  }
}
