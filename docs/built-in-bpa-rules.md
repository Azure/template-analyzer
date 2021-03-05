# Security Related Rules
## Automation account variables should be encrypted
It is important to enable encryption of Automation account variable assets when storing sensitive data. This step can only be taken at creation time. If you have Automation Account Variables storing sensitive data that are not already encrypted, then you will need to delete them and recreate them as encrypted variables. To apply encryption of the Automation account variable assets, in Azure PowerShell - run [the following command](https://docs.microsoft.com/en-us/powershell/module/az.automation/set-azautomationvariable?view=azps-5.4.0&viewFallbackFrom=azps-1.4.0): `Set-AzAutomationVariable -AutomationAccountName '{AutomationAccountName}' -Encrypted $true -Name '{VariableName}' -ResourceGroupName '{ResourceGroupName}' -Value '{Value}'`

**Recommendation**: [Enable encryption of Automation account variable assets](https://docs.microsoft.com/en-us/azure/automation/shared-resources/variables?tabs=azure-powershell)

## Role-Based Access Control (RBAC) should be used on Kubernetes Services
To provide granular filtering on the actions that users can perform, use Role-Based Access Control (RBAC) to manage permissions in Kubernetes Service Clusters and configure relevant authorization policies. To Use Role-Based Access Control (RBAC) you must recreate your Kubernetes Service cluster and enable RBAC during the creation process.

**Recommendation**: [Enable RBAC in Kubernetes clusters](https://docs.microsoft.com/en-us/azure/aks/operator-best-practices-identity#use-azure-rbac)

## Service Fabric clusters should only use Azure Active Directory for client authentication
Service Fabric clusters should only use Azure Active Directory for client authentication. A Service Fabric cluster offers several entry points to its management functionality, including the web-based Service Fabric Explorer, Visual Studio and PowerShell. Access to the cluster must be controlled using AAD.

**Recommendation**: [Enable AAD client authentication on your Service Fabric clusters](https://docs.microsoft.com/en-in/azure/service-fabric/service-fabric-cluster-creation-setup-aad)

## Use built-in roles instead of custom RBAC roles
You should only use built-in roles instead of cutom RBAC roles. Custom RBAC roles are error prone. Using custom roles is treated as an exception and requires a rigorous review and threat modeling.

**Recommendation**: [Use built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)