@description('Location for all resources.')
param location string = resourceGroup().location

resource serverFarm 'Microsoft.Web/serverfarms@2019-08-01' = {
  name: 'serverFarm'
  location: location
}

resource ApiAppNoHttps 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiAppNoHttps'
  location: location
  properties: {
    serverFarmId: serverFarm.id
  }
}

resource ApiApp_HttpsFalse 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiApp_HttpsFalse'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: false
  }
}

resource ApiApp_HttpsTrue 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiApp_HttpsTrue'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: true
  }
}

resource FunctionAppNoHttps 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'FunctionAppNoHttps'
  location: location
  properties: {
    serverFarmId: serverFarm.id
  }
}

resource FunctionApp_HttpsFalse 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp,linux'
  name: 'FunctionApp_HttpsFalse'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: false
  }
}

resource FunctionApp_HttpsTrue 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'FunctionApp_HttpsTrue'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: true
  }
}

resource WebAppNoHttps 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app,linux'
  name: 'WebAppNoHttps'
  location: location
  properties: {
    serverFarmId: serverFarm.id
  }
}

resource WebApp_HttpsFalse 'Microsoft.Web/sites@2019-08-01' = {
  name: 'WebApp_HttpsFalse'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: false
  }
}

resource WebApp_HttpsTrue 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'WebApp_HttpsTrue'
  location: location
  properties: {
    serverFarmId: serverFarm.id
    httpsOnly: true
  }
}

resource ApiApp_RestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiApp_RestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
        ]
      }
    }
  }
}

resource ApiApp_NoSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiApp_NoSitesConfig'
  location: location
  properties: {
    httpsOnly: true
  }
}

resource SitesConfig_RestrictedCORSAccess_web 'Microsoft.Web/sites/config@2019-08-01' = {
  name: 'SitesConfig/RestrictedCORSAccess_web'
  location: location
  properties: {
    cors: {
      allowedOrigins: [
        'someIP'
      ]
    }
  }
  dependsOn: [
    ApiApp_NoSitesConfig
    WebApp_NoSitesConfig
    FunctionApp_NoSitesConfig
  ]
}

resource ApiApp_UnrestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'api'
  name: 'ApiApp_UnrestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
          '*'
        ]
      }
    }
  }
}

resource SitesConfig_UnrestrictedCORSAccess_web 'Microsoft.Web/sites/config@2019-08-01' = {
  name: 'SitesConfig/UnrestrictedCORSAccess_web'
  location: location
  properties: {
    cors: {
      allowedOrigins: [
        '*'
      ]
    }
  }
  dependsOn: [
    ApiApp_NoSitesConfig
    WebApp_NoSitesConfig
    FunctionApp_NoSitesConfig
  ]
}

resource WebApp_RestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'WebApp_RestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
        ]
      }
    }
  }
}

resource WebApp_NoKind_RestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  name: 'WebApp_NoKind_RestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
        ]
      }
    }
  }
}

resource WebApp_UnrestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'WebApp_UnrestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
          '*'
        ]
      }
    }
  }
}

resource WebApp_NoSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'app'
  name: 'WebApp_NoSitesConfig'
  location: location
  properties: {
    httpsOnly: true
  }
}

resource FunctionApp_RestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'FunctionApp_RestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
        ]
      }
    }
  }
}

resource FunctionApp_UnrestrictedCORSAccess_EmbeddedSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'FunctionApp_UnrestrictedCORSAccess_EmbeddedSitesConfig'
  location: location
  properties: {
    httpsOnly: true
    siteConfig: {
      cors: {
        allowedOrigins: [
          'someIP'
          '*'
        ]
      }
    }
  }
}

resource FunctionApp_NoSitesConfig 'Microsoft.Web/sites@2019-08-01' = {
  kind: 'functionapp'
  name: 'FunctionApp_NoSitesConfig'
  location: location
  properties: {
    httpsOnly: true
  }
}