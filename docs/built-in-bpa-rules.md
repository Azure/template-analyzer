# Security Related Rules
## API App Should Only Be Accessible Over HTTPS
API Apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.

## Authorized IP ranges should be defined on Kubernetes Services
To ensure that only applications from allowed networks, machines, or subnets can access your cluster, restrict access to your Kubernetes Service Management API server. It is recommended to limit access to authorized IP ranges to ensure that only applications from allowed networks can access the cluster.

**Recommendation**: [Restrict access by defining authorized IP ranges](https://docs.microsoft.com/en-us/azure/aks/api-server-authorized-ip-ranges) or [set up your API servers as private clusters](https://docs.microsoft.com/azure/aks/private-clusters)

## Automation account variables should be encrypted
It is important to enable encryption of Automation account variable assets when storing sensitive data. This step can only be taken at creation time. If you have Automation Account Variables storing sensitive data that are not already encrypted, then you will need to delete them and recreate them as encrypted variables. To apply encryption of the Automation account variable assets, in Azure PowerShell - run [the following command](https://docs.microsoft.com/en-us/powershell/module/az.automation/set-azautomationvariable?view=azps-5.4.0&viewFallbackFrom=azps-1.4.0): `Set-AzAutomationVariable -AutomationAccountName '{AutomationAccountName}' -Encrypted $true -Name '{VariableName}' -ResourceGroupName '{ResourceGroupName}' -Value '{Value}'`

**Recommendation**: [Enable encryption of Automation account variable assets](https://docs.microsoft.com/en-us/azure/automation/shared-resources/variables?tabs=azure-powershell)

## Function App Should Only Be Accessible Over HTTPS
Function Apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.

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
Web Apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.