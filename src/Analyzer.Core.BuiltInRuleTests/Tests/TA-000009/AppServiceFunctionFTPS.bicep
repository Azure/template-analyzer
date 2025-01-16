@description('Location for all resources.')
param location string = resourceGroup().location

// Positive Case: FTPS only is enforced for function app
@description('Function App with FTPS only enabled (positive case)')
resource functionAppPositive 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-positive'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      ftpsState: 'FtpsOnly' // FTPS only is enforced
    }
  }
}

// Positive Case: FTPS is disabled for function app (valid case)
@description('Function App with FTPS disabled (positive case)')
resource functionAppPositiveDisabled 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-positive-disabled'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      ftpsState: 'Disabled' // FTPS is disabled
    }
  }
}

// Positive Case: FTPS only enforced in site config for function app
@description('Function App with FTPS only enforced in site config (positive case)')
resource functionAppPositiveConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppPositive
  name: 'web'
  properties: {
    ftpsState: 'FtpsOnly' // FTPS only is enforced
  }
}

// Negative Case: FTPS is not enforced for function app
@description('Function App without FTPS enforcement (negative case)')
resource functionAppNegative 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-negative'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      ftpsState: 'AllAllowed' // FTPS enforcement is not set (violation)
    }
  }
}

// Negative Case: FTPS is not enforced in site config for function app
@description('Function App without FTPS enforcement in site config (negative case)')
resource functionAppNegativeFTPSConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppNegative
  name: 'web'
  properties: {
    ftpsState: 'AllAllowed' // FTPS enforcement is not set (violation)
  }
}

// Negative Case: FTPS state is not set for function app
@description('Function App without FTPS state set (negative case)')
resource functionAppNoFTPSState 'Microsoft.Web/sites@2022-03-01' = {
  name: 'function-app-no-ftps-state'
  location: location
  kind: 'functionapp'
  properties: {
    siteConfig: {
      // FTPS state is not set
    }
  }
}

// Negative Case: FTPS state is not set in site config for function app
@description('Function App without FTPS state set in site config (negative case)')
resource functionAppNoFTPSConfig 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: functionAppNoFTPSState
  name: 'web'
  properties: {
    // FTPS state is not set
  }
}
