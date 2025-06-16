resource functionAppTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppTls12'
  properties: {
    siteConfig: {
      minTlsVersion: '1.2'
      }
  }
}

resource functionAppTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppTls13'
  properties: {
    siteConfig: {
      minTlsVersion: '1.3'
      }
  }
}

resource functionAppSeparateConfigTls12 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppSeparateConfigTls12'
}

resource functionAppConfigTls12 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionAppSeparateConfigTls12
  name: 'web'
  properties: {
    minTlsVersion: '1.2'
  }
}

resource functionAppSeparateConfigTls13 'Microsoft.Web/sites@2022-09-01' = {
  kind: 'functionapp'
  name: 'functionAppSeparateConfigTls13'
}

resource functionAppConfigTls13 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: functionAppSeparateConfigTls13
  name: 'web'
  properties: {
    minTlsVersion: '1.3'
  }
}
