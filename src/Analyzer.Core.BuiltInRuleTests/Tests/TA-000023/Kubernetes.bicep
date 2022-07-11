@description('Location for all resources.')
param location string = resourceGroup().location

resource AKS_AuthorizedIPs 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_AuthorizedIPs'
  location: location
  properties: {
    apiServerAccessProfile: {
      authorizedIPRanges: [
        'IP1'
      ]
    }
  }
}

resource AKS_AuthorizedIPsEmpty 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_AuthorizedIPsEmpty'
  location: location
  properties: {
    apiServerAccessProfile: {
      authorizedIPRanges: []
    }
  }
}

resource AKS_NoIPRestrictions 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_NoIPRestrictions'
  location: location
  properties: {
    apiServerAccessProfile: {
    }
  }
}

resource AKS_EnablePrivateClusterTrue 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_EnablePrivateClusterTrue'
  location: location
  properties: {
    apiServerAccessProfile: {
      enablePrivateCluster: true
    }
  }
}

resource AKS_EnablePrivateClusterFalse 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_EnablePrivateClusterFalse'
  location: location
  properties: {
    apiServerAccessProfile: {
      enablePrivateCluster: false
    }
  }
}

resource AKS_EnablePrivateClusterFalse_AuthorizedIPs 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_EnablePrivateClusterFalse_AuthorizedIPs'
  location: location
  properties: {
    apiServerAccessProfile: {
      enablePrivateCluster: false
      authorizedIPRanges: [
        'IP1'
      ]
    }
  }
}