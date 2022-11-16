resource aClassicComputeVM 'Microsoft.ClassicCompute/virtualMachines@2021-08-01' = {
  name: 'aClassicComputeVM'
  properties: {
  }
}

resource anotherTypeOfVM 'Microsoft.Compute/virtualMachines@2022-03-01' = {
  name: 'anotherTypeOfVM'
  properties: {
  }
}