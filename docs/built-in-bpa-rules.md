# Security Related Rules
## Service Fabric clusters should only use Azure Active Directory for client authentication
Service Fabric clusters should only use Azure Active Directory for client authentication. A Service Fabric cluster offers several entry points to its management functionality, including the web-based Service Fabric Explorer, Visual Studio and PowerShell. Access to the cluster must be controlled using AAD.

**Recommendation**: [Enable AAD client authentication on your Service Fabric clusters](https://docs.microsoft.com/en-in/azure/service-fabric/service-fabric-cluster-creation-setup-aad)

## Use built-in roles instead of custom RBAC roles
You should only use built-in roles instead of cutom RBAC roles. Custom RBAC roles are error prone. Using custom roles is treated as an exception and requires a rigorous review and threat modeling.

**Recommendation**: [Use built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles
)
