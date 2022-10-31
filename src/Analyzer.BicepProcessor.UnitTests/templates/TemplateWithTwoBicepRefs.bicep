@description('Storage account name')
param location string = 'testLocation'

module nestedTemplate './TemplateWithTwoArmRefs.bicep' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplate2 './TemplateWithTwoArmRefs.bicep' = {
  name: 'nestedTemplate2'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

