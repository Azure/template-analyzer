# Best Practice Related Rules
## Ensure all Virtual Machines are not using preview images
Virtual machines should not use preview versions of images.

**Recommendation**: To use a non-preview image for virtual machines, in the [Microsoft.Compute/VirtualMachines/imageReference resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.compute/virtualmachines?tabs=json#ImageReference), ensure the *version* property does not contain "-preview".

## Ensure all Virtual Machine Scale Set instances are not using preview images
Virtual machine scale set instances should not use preview versions of images.

**Recommendation**: To use a non-preview image for virtual machine scale set instances, in the [Microsoft.Compute/VirtualMachineScaleSets/storageProfile.imageReference resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.compute/virtualmachinescalesets?tabs=json#ImageReference), ensure the *version* property does not contain "-preview".

## Azure Resource Manager Template Toolkit rules
More information about the rules covered by our integration with [arm-ttk](https://github.com/Azure/arm-ttk) can be found [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/test-cases):

#### DeploymentTemplate schema is correct
#### Parameters must be referenced
#### Secure string parameters cannot have defaults
#### DeploymentTemplate must not contain hardcoded URI
#### Location should not be hardcoded
#### Resources should have locations
#### VM size should be a parameter
#### Min and max values are numbers
#### artifacts parameter
#### Variables must be referenced
#### Dynamic variable references should not use concat
#### apiVersions should be Rrcent
#### Providers apiVersions is not permitted
#### Template should not contain blanks
#### IDs should be derived from resourceIDs
#### ResourceIDs should not contain
#### DependsOn' best practices
#### Deployment resources must not be debug
#### adminUsername should not be a literal
#### VM images should use latest version
#### Virtual machines should not be preview
#### ManagedIdentityExtension must not be used
#### Outputs must not contain secrets
#### CommandToExecute must use ProtectedSettings for secrets
#### Resources should not be ambiguous

# Security Related Rules
## API App Should Only Be Accessible Over HTTPS
API apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.

## Authorized IP ranges should be defined on Kubernetes Services
To ensure that only applications from allowed networks, machines, or subnets can access your cluster, restrict access to your Kubernetes Service Management API server. It is recommended to limit access to authorized IP ranges to ensure that only applications from allowed networks can access the cluster.

**Recommendation**: [Restrict access by defining authorized IP ranges](https://docs.microsoft.com/en-us/azure/aks/api-server-authorized-ip-ranges) or [set up your API servers as private clusters](https://docs.microsoft.com/azure/aks/private-clusters)

## Automation account variables should be encrypted
It is important to enable encryption of Automation account variable assets when storing sensitive data. This step can only be taken at creation time. If you have Automation Account Variables storing sensitive data that are not already encrypted, then you will need to delete them and recreate them as encrypted variables. To apply encryption of the Automation account variable assets, in Azure PowerShell - run [the following command](https://docs.microsoft.com/en-us/powershell/module/az.automation/set-azautomationvariable?view=azps-5.4.0&viewFallbackFrom=azps-1.4.0): `Set-AzAutomationVariable -AutomationAccountName '{AutomationAccountName}' -Encrypted $true -Name '{VariableName}' -ResourceGroupName '{ResourceGroupName}' -Value '{Value}'`

**Recommendation**: [Enable encryption of Automation account variable assets](https://docs.microsoft.com/en-us/azure/automation/shared-resources/variables?tabs=azure-powershell)

## Function App Should Only Be Accessible Over HTTPS
Function apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.

## Latest TLS version should be used in your Web App
Web apps should require the latest TLS version.

**Recommendation**: 
To [enforce the latest TLS version](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.

## Only secure connections to your Azure Cache for Redis should be enabled
Enable only connections via SSL to Redis Cache. Use of secure connections ensures authentication between the server and the service and protects data in transit from network layer attacks such as man-in-the-middle, eavesdropping, and session-hijacking.

**Recommendation**: To [enable only connections via SSL to Redis Cache](https://docs.microsoft.com/en-us/security/benchmark/azure/baselines/azure-cache-for-redis-security-baseline?toc=/azure/azure-cache-for-redis/TOC.json#44-encrypt-all-sensitive-information-in-transit), in the [Microsoft.Cache/Redis resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.cache/redis?tabs=json#rediscreateproperties-object), update the value of the *enableNonSslPort* property from `true` to `false` or remove the property from the template as the default value is `false`.

## Role-Based Access Control (RBAC) should be used on Kubernetes Services
To provide granular filtering on the actions that users can perform, use Role-Based Access Control (RBAC) to manage permissions in Kubernetes Service Clusters and configure relevant authorization policies. To Use Role-Based Access Control (RBAC) you must recreate your Kubernetes Service cluster and enable RBAC during the creation process.

**Recommendation**: [Enable RBAC in Kubernetes clusters](https://docs.microsoft.com/en-us/azure/aks/operator-best-practices-identity#use-azure-rbac)

## Service Fabric clusters should only use Azure Active Directory for client authentication
Service Fabric clusters should only use Azure Active Directory for client authentication. A Service Fabric cluster offers several entry points to its management functionality, including the web-based Service Fabric Explorer, Visual Studio and PowerShell. Access to the cluster must be controlled using AAD.

**Recommendation**: [Enable AAD client authentication on your Service Fabric clusters](https://docs.microsoft.com/en-in/azure/service-fabric/service-fabric-cluster-creation-setup-aad)

## Use built-in roles instead of custom RBAC roles
You should only use built-in roles instead of cutom RBAC roles. Custom RBAC roles are error prone. Using custom roles is treated as an exception and requires a rigorous review and threat modeling.

**Recommendation**: [Use built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)

## Web Application Should Only Be Accessible Over HTTPS
Web apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.