@description('Location for all resources.')
param location string = resourceGroup().location

resource AKS_Non_vulnerableVersion_1_13_5 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_Non-vulnerableVersion_1.13.5+'
  location: location
  properties: {
    kubernetesVersion: '1.13.5'
  }
}

resource AKS_VulnerableVersion_1_13_0 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.13.0'
  location: location
  properties: {
    kubernetesVersion: '1.13.0'
  }
}

resource AKS_Non_vulnerableVersion_1_12_7 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_Non-vulnerableVersion_1.12.7+'
  location: location
  properties: {
    kubernetesVersion: '1.12.7'
  }
}

resource AKS_VulnerableVersion_1_12_1 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.12.1'
  location: location
  properties: {
    kubernetesVersion: '1.12.1'
  }
}

resource AKS_Non_vulnerableVersion_1_11_9 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_Non-vulnerableVersion_1.11.9+'
  location: location
  properties: {
    kubernetesVersion: '1.11.9'
  }
}

resource AKS_VulnerableVersion_1_11_2 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.11.2'
  location: location
  properties: {
    kubernetesVersion: '1.11.2'
  }
}

resource AKS_VulnerableVersion_1_10_2 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.10.2'
  location: location
  properties: {
    kubernetesVersion: '1.10.2'
  }
}

resource AKS_VulnerableVersion_1_9_4 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.9.4'
  location: location
  properties: {
    kubernetesVersion: '1.9.4'
  }
}

resource AKS_VulnerableVersion_1_8_1 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.8.1'
  location: location
  properties: {
    kubernetesVersion: '1.8.1'
  }
}

resource AKS_VulnerableVersion_1_7_0 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.7.0'
  location: location
  properties: {
    kubernetesVersion: '1.7.0'
  }
}

resource AKS_VulnerableVersion_1_6_7 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.6.7'
  location: location
  properties: {
    kubernetesVersion: '1.6.7'
  }
}

resource AKS_VulnerableVersion_1_5_3 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.5.3'
  location: location
  properties: {
    kubernetesVersion: '1.5.3'
  }
}

resource AKS_VulnerableVersion_1_4_7 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.4.7'
  location: location
  properties: {
    kubernetesVersion: '1.4.7'
  }
}

resource AKS_VulnerableVersion_1_3_4 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.3.4'
  location: location
  properties: {
    kubernetesVersion: '1.3.4'
  }
}

resource AKS_VulnerableVersion_1_2_9 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.2.9'
  location: location
  properties: {
    kubernetesVersion: '1.2.9'
  }
}

resource AKS_VulnerableVersion_1_1_10 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.1.10'
  location: location
  properties: {
    kubernetesVersion: '1.1.10'
  }
}

resource AKS_VulnerableVersion_1_0_5 'Microsoft.ContainerService/managedClusters@2020-07-01' = {
  name: 'AKS_VulnerableVersion_1.0.5'
  location: location
  properties: {
    kubernetesVersion: '1.0.5'
  }
}