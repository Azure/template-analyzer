param infra object

resource applicationInsight 'microsoft.insights/components@2020-02-02' = {
  name: 'test'
  location: infra.environment.location
  kind: 'other'
}