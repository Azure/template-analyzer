@description('Storage account name')
param location string = 'testLocation'

module nestedTemplate './AppServicesLogs-Failures.json' = {
  name: 'nestedTemplate'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplate2 './AppServicesLogs-Failures.bicep' = {
  name: 'nestedTemplate2'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplate3 './TemplateWithTwoArmRefs.bicep' = {
  name: 'nestedTemplate3'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

module nestedTemplat43 './TemplateWithTwoBicepRefs.bicep' = {
  name: 'nestedTemplate4'
  scope: resourceGroup('my-rg')
  params: {
    location: location
  }
}

//module hw 'br/public:samples/hello-world:1.0.2' = {
//  name: 'hello-world'
//  params: {
//    name: 'Template Analyzer'
//  }
//}
