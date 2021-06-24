# Best Practice Related Rules
More information about the rules covered by our integration with [Azure Resource Manager Template Toolkit](https://github.com/Azure/arm-ttk) can be found [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/templates/test-cases):

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

## CORS should not allow every resource to access your API App
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your Web application. Allow only required domains to interact with your api app.

**Recommendation**: To allow only required domains to interact with your web app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).

## CORS should not allow every resource to access your Function Apps
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your Web application. Allow only required domains to interact with your function app.

**Recommendation**: To allow only required domains to interact with your web app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).

## CORS should not allow every resource to access your Web Application
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your Web application. Allow only required domains to interact with your web app.

**Recommendation**: To allow only required domains to interact with your web app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).

## Diagnostic logs in App Services should be enabled
Audit enabling of diagnostic logs on the app. This enables you to recreate activity trails for investigation purposes if a security incident occurs or your network is compromised.

**Recommendation**: To [enable diagnostic logging](https://docs.microsoft.com/en-us/azure/app-service/troubleshoot-diagnostic-logs), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *detailedErrorLoggingEnabled*, *httpLoggingEnabled*, and *requestTracingEnabled* properties, setting their values to `true`.

## FTPS only should be required in your API App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/en-us/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.

## FTPS only should be required in your Function App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/en-us/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.

## FTPS only should be required in your Web App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/en-us/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.

## Function App Should Only Be Accessible Over HTTPS
Function apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.

## Latest TLS version should be used in your API App
API apps should require the latest TLS version.

**Recommendation**: 
To [enforce the latest TLS version](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.

## Latest TLS version should be used in your Function App
Function apps should require the latest TLS version.

**Recommendation**: 
To [enforce the latest TLS version](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.

## Latest TLS version should be used in your Web App
Web apps should require the latest TLS version.

**Recommendation**: 
To [enforce the latest TLS version](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.

## Managed identity should be used in your API App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.

## Managed identity should be used in your Function App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.

## Managed identity should be used in your Web App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.

## Only secure connections to your Azure Cache for Redis should be enabled
Enable only connections via SSL to Redis Cache. Use of secure connections ensures authentication between the server and the service and protects data in transit from network layer attacks such as man-in-the-middle, eavesdropping, and session-hijacking.

**Recommendation**: To [enable only connections via SSL to Redis Cache](https://docs.microsoft.com/en-us/security/benchmark/azure/baselines/azure-cache-for-redis-security-baseline?toc=/azure/azure-cache-for-redis/TOC.json#44-encrypt-all-sensitive-information-in-transit), in the [Microsoft.Cache/Redis resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.cache/redis?tabs=json#rediscreateproperties-object), update the value of the *enableNonSslPort* property from `true` to `false` or remove the property from the template as the default value is `false`.

## Remote debugging should be turned off for API Apps
Remote debugging requires inbound ports to be opened on an api app. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.

## Remote debugging should be turned off for Function Apps
Remote debugging requires inbound ports to be opened on a function app. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.

## Remote debugging should be turned off for Web Applications
Remote debugging requires inbound ports to be opened on a web application. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.

## Role-Based Access Control (RBAC) should be used on Kubernetes Services
To provide granular filtering on the actions that users can perform, use Role-Based Access Control (RBAC) to manage permissions in Kubernetes Service Clusters and configure relevant authorization policies. To Use Role-Based Access Control (RBAC) you must recreate your Kubernetes Service cluster and enable RBAC during the creation process.

**Recommendation**: [Enable RBAC in Kubernetes clusters](https://docs.microsoft.com/en-us/azure/aks/operator-best-practices-identity#use-azure-rbac)

## Service Fabric clusters should only use Azure Active Directory for client authentication
Service Fabric clusters should only use Azure Active Directory for client authentication. A Service Fabric cluster offers several entry points to its management functionality, including the web-based Service Fabric Explorer, Visual Studio and PowerShell. Access to the cluster must be controlled using AAD.

**Recommendation**: [Enable AAD client authentication on your Service Fabric clusters](https://docs.microsoft.com/en-in/azure/service-fabric/service-fabric-cluster-creation-setup-aad)

## Transparent Data Encryption on SQL databases should be enabled
Transparent data encryption should be enabled to protect data-at-rest and meet compliance requirements.

**Recommendation**: To [enable transparent data encryption](https://docs.microsoft.com/en-us/azure/azure-sql/database/transparent-data-encryption-tde-overview?tabs=azure-portal), in the [Microsoft.Sql/servers/databases/transparentDataEncryption resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.sql/servers/databases/transparentdataencryption?tabs=json), add (or update) the value of the *state* property to `enabled`.

## Use built-in roles instead of custom RBAC roles
You should only use built-in roles instead of cutom RBAC roles. Custom RBAC roles are error prone. Using custom roles is treated as an exception and requires a rigorous review and threat modeling.

**Recommendation**: [Use built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles](https://docs.microsoft.com/en-us/azure/role-based-access-control/built-in-roles)

## Web Application Should Only Be Accessible Over HTTPS
Web apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/en-us/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/en-us/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.