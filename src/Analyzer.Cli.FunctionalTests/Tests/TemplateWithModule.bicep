module testModule 'AppServicesLogs-Failures.bicep' = {
	name:'testModuleDeployment'
	params: {
		location: 'testLocation'
	}
}

module hw 'br/public:samples/hello-world:1.0.2' = {
  name: 'hello-world'
  params: {
    name: 'Template Analyzer'
  }
}
