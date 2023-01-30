@description('The name of the API Management service instance')
param apiManagementServiceName string = 'apiservice${uniqueString(resourceGroup().id)}'

@description('The email address of the owner of the service')
@minLength(1)
param publisherEmail string = 'test@test.com'

@description('The name of the owner of the service')
@minLength(1)
param publisherName string = 'test test'

@description('The pricing tier of this API Management service')
@allowed([
  'Developer'
  'Standard'
  'Premium'
])
param sku string = 'Developer'

@description('The instance size of this API Management service.')
@allowed([
  1
  2
])
param skuCount int = 1

@description('Location for all resources.')
param location string = resourceGroup().location

resource apiManagementService 'Microsoft.ApiManagement/service@2021-08-01' = {
  name: apiManagementServiceName
  location: location
  sku: {
    name: sku
    capacity: skuCount
  }
  properties: {
    publisherEmail: publisherEmail
    publisherName: publisherName
  }
}

resource apiManagementServiceName_httpsonly 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'httpsonly'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'https'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_missingprotocols 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'missingprotocols'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    isCurrent: true
  }
}

resource apiManagementServiceName_httponly 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'httponly'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'http'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_wsonly 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'wsonly'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'ws'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_httpandws 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'httpandws'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'http'
      'ws'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_httpandhttps 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'httpandhttps'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'http'
      'https'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_wsandhttps 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'wsandhttps'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: [
      'ws'
      'https'
    ]
    isCurrent: true
  }
}

resource apiManagementServiceName_emptyprotocols 'Microsoft.ApiManagement/service/apis@2021-12-01-preview' = {
  parent: apiManagementService
  name: 'emptyprotocols'
  properties: {
    displayName: 'apimhelloworld'
    apiRevision: '1'
    description: 'Import from "apimhelloworld" Function App'
    subscriptionRequired: true
    path: 'apimhelloworld'
    protocols: []
    isCurrent: true
  }
}