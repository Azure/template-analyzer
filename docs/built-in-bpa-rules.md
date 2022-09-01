# Built-in Rules

## PowerShell-Based Rules:

Information about the PowerShell-based rules included by our integration with [PSRule for Azure](https://aka.ms/ps-rule-azure/rules). The tool will only evaluate the rules under the [Security pillar](https://azure.github.io/PSRule.Rules.Azure/en/rules/module/#security) unless the option `--include-non-security-rules` is used.

## JSON-Based Rules:

### TA-000001: Diagnostic logs in App Services should be enabled
Audit enabling of diagnostic logs on the app. This enables you to recreate activity trails for investigation purposes if a security incident occurs or your network is compromised.

**Recommendation**: To [enable diagnostic logging](https://docs.microsoft.com/azure/app-service/troubleshoot-diagnostic-logs), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *detailedErrorLoggingEnabled*, *httpLoggingEnabled*, and *requestTracingEnabled* properties, setting their values to `true`.
#### Severity: 2

### TA-000002: Remote debugging should be turned off for API Apps
Remote debugging requires inbound ports to be opened on an API app. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.
#### Severity: 3

### TA-000003: FTPS only should be required in your API App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.
#### Severity: 1

### TA-000004: API App Should Only Be Accessible Over HTTPS
API apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.
#### Severity: 2

### TA-000005: Latest TLS version should be used in your API App
API apps should require the latest TLS version.

**Recommendation**: To [enforce the latest TLS version](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.
#### Severity: 1

### TA-000006: CORS should not allow every resource to access your API App
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your API app. Allow only required domains to interact with your API app.

**Recommendation**: To allow only required domains to interact with your API app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).
#### Severity: 3

### TA-000007: Managed identity should be used in your API App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.
#### Severity: 2

### TA-000008: Remote debugging should be turned off for Function Apps
Remote debugging requires inbound ports to be opened on a function app. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.
#### Severity: 3

### TA-000009: FTPS only should be required in your Function App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.
#### Severity: 1

### TA-000010: Function App Should Only Be Accessible Over HTTPS
Function apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.
#### Severity: 2

### TA-000011: Latest TLS version should be used in your Function App
Function apps should require the latest TLS version.

**Recommendation**: To [enforce the latest TLS version](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.
#### Severity: 1

### TA-000012: CORS should not allow every resource to access your Function Apps
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your function app. Allow only required domains to interact with your function app.

**Recommendation**: To allow only required domains to interact with your function app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).
#### Severity: 3

### TA-000013: Managed identity should be used in your Function App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.
#### Severity: 2

### TA-000014: Remote debugging should be turned off for Web Applications
Remote debugging requires inbound ports to be opened on a web application. These ports become easy targets for compromise from various internet based attacks. If you no longer need to use remote debugging, it should be turned off.

**Recommendation**: To disable remote debugging, in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), remove the *remoteDebuggingEnabled* property or update its value to `false`.
#### Severity: 3

### TA-000015: FTPS only should be required in your Web App
Enable FTPS enforcement for enhanced security.

**Recommendation**: To [enforce FTPS](https://docs.microsoft.com/azure/app-service/deploy-ftp?tabs=portal#enforce-ftps), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *ftpsState* property, setting its value to `"FtpsOnly"` or `"Disabled"` if you don't need FTPS enabled.
#### Severity: 1

### TA-000016: Web Application Should Only Be Accessible Over HTTPS
Web apps should require HTTPS to ensure connections are made to the expected server and data in transit is protected from network layer eavesdropping attacks.

**Recommendation**: To [use HTTPS to ensure server/service authentication and protect data in transit from network layer eavesdropping attacks](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-https), in the [Microsoft.Web/Sites resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#siteproperties-object), add (or update) the *httpsOnly* property, setting its value to `true`.
#### Severity: 2

### TA-000017: Latest TLS version should be used in your Web App
Web apps should require the latest TLS version.

**Recommendation**: 
To [enforce the latest TLS version](https://docs.microsoft.com/azure/app-service/configure-ssl-bindings#enforce-tls-versions), in the [Microsoft.Web/sites/config resource properties](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#SiteConfig), add (or update) the *minTlsVersion* property, setting its value to `1.2`.
#### Severity: 1

### TA-000018: CORS should not allow every resource to access your Web Applications
Cross-Origin Resource Sharing (CORS) should not allow all domains to access your Web application. Allow only required domains to interact with your web app.

**Recommendation**: To allow only required domains to interact with your web app, in the [Microsoft.Web/sites/config resource cors settings object](https://docs.microsoft.com/azure/templates/microsoft.web/sites/config-web?tabs=json#corssettings-object), add (or update) the *allowedOrigins* property, setting its value to an array of allowed origins. Ensure it is *not* set to "*" (asterisks allows all origins).
#### Severity: 3

### TA-000019: Managed identity should be used in your Web App
For enhanced authentication security, use a managed identity. On Azure, managed identities eliminate the need for developers to have to manage credentials by providing an identity for the Azure resource in Azure AD and using it to obtain Azure Active Directory (Azure AD) tokens.

**Recommendation**: To [use Managed Identity](https://docs.microsoft.com/azure/app-service/overview-managed-identity?tabs=dotnet), in the [Microsoft.Web/sites resource managed identity property](https://docs.microsoft.com/azure/templates/microsoft.web/sites?tabs=json#ManagedServiceIdentity), add (or update) the *type* property, setting its value to `"SystemAssigned"` or `"UserAssigned"` and providing any necessary identifiers for the identity if required.
#### Severity: 2

### TA-000020: Audit usage of custom RBAC roles
Audit built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles, which are error prone. Using custom roles is treated as an exception and requires a rigorous review and threat modeling.

**Recommendation**: [Use built-in roles such as 'Owner, Contributer, Reader' instead of custom RBAC roles](https://docs.microsoft.com/azure/role-based-access-control/built-in-roles)
#### Severity: 3

### TA-000021: Automation account variables should be encrypted
It is important to enable encryption of Automation account variable assets when storing sensitive data. This step can only be taken at creation time. If you have Automation Account Variables storing sensitive data that are not already encrypted, then you will need to delete them and recreate them as encrypted variables. To apply encryption of the Automation account variable assets, in Azure PowerShell - run [the following command](https://docs.microsoft.com/powershell/module/az.automation/set-azautomationvariable?view=azps-5.4.0&viewFallbackFrom=azps-1.4.0): `Set-AzAutomationVariable -AutomationAccountName '{AutomationAccountName}' -Encrypted $true -Name '{VariableName}' -ResourceGroupName '{ResourceGroupName}' -Value '{Value}'`

**Recommendation**: [Enable encryption of Automation account variable assets](https://docs.microsoft.com/azure/automation/shared-resources/variables?tabs=azure-powershell)
#### Severity: 1

### TA-000022: Only secure connections to your Azure Cache for Redis should be enabled
Enable only connections via SSL to Redis Cache. Use of secure connections ensures authentication between the server and the service and protects data in transit from network layer attacks such as man-in-the-middle, eavesdropping, and session-hijacking.

**Recommendation**: To [enable only connections via SSL to Redis Cache](https://docs.microsoft.com/security/benchmark/azure/baselines/azure-cache-for-redis-security-baseline?toc=/azure/azure-cache-for-redis/TOC.json#44-encrypt-all-sensitive-information-in-transit), in the [Microsoft.Cache/Redis resource properties](https://docs.microsoft.com/azure/templates/microsoft.cache/redis?tabs=json#rediscreateproperties-object), update the value of the *enableNonSslPort* property from `true` to `false` or remove the property from the template as the default value is `false`.
#### Severity: 1

### TA-000023: Authorized IP ranges should be defined on Kubernetes Services
To ensure that only applications from allowed networks, machines, or subnets can access your cluster, restrict access to your Kubernetes Service Management API server. It is recommended to limit access to authorized IP ranges to ensure that only applications from allowed networks can access the cluster.

**Recommendation**: [Restrict access by defining authorized IP ranges](https://docs.microsoft.com/azure/aks/api-server-authorized-ip-ranges) or [set up your API servers as private clusters](https://docs.microsoft.com/azure/aks/private-clusters)
#### Severity: 1

### TA-000024: Role-Based Access Control (RBAC) should be used on Kubernetes Services
To provide granular filtering on the actions that users can perform, use Role-Based Access Control (RBAC) to manage permissions in Kubernetes Service Clusters and configure relevant authorization policies. To Use Role-Based Access Control (RBAC) you must recreate your Kubernetes Service cluster and enable RBAC during the creation process.

**Recommendation**: [Enable RBAC in Kubernetes clusters](https://docs.microsoft.com/azure/aks/operator-best-practices-identity#use-azure-rbac)
#### Severity: 1

### TA-000025: Kubernetes Services should be upgraded to a non-vulnerable Kubernetes version
Upgrade your Kubernetes service cluster to a later Kubernetes version to protect against known vulnerabilities in your current Kubernetes version. [Vulnerability CVE-2019-9946](https://cve.mitre.org/cgi-bin/cvename.cgi?name=CVE-2019-9946) has been patched in Kubernetes versions 1.11.9+, 1.12.7+, 1.13.5+, and 1.14.0+. Running on older versions could mean you are not using latest security classes. Usage of such old classes and types can make your application vulnerable.

**Recommendation**: To [upgrade Kubernetes service clusters](https://docs.microsoft.com/azure/aks/upgrade-cluster), in the [Microsoft.ContainerService/managedClusters resource properties](https://docs.microsoft.com/azure/templates/microsoft.containerservice/managedclusters?tabs=json#managedclusterproperties-object), update the *kubernetesVersion* property, setting its value to one of the following versions (making sure to specify the minor version number): 1.11.9+, 1.12.7+, 1.13.5+, or 1.14.0+.
#### Severity: 1

### TA-000026: Service Fabric clusters should only use Azure Active Directory for client authentication
Service Fabric clusters should only use Azure Active Directory for client authentication. A Service Fabric cluster offers several entry points to its management functionality, including the web-based Service Fabric Explorer, Visual Studio and PowerShell. Access to the cluster must be controlled using AAD.

**Recommendation**: [Enable AAD client authentication on your Service Fabric clusters](https://docs.microsoft.com/azure/service-fabric/service-fabric-cluster-creation-setup-aad)
#### Severity: 1

### TA-000027: Transparent Data Encryption on SQL databases should be enabled
Transparent data encryption should be enabled to protect data-at-rest and meet compliance requirements.

**Recommendation**: To [enable transparent data encryption](https://docs.microsoft.com/azure/azure-sql/database/transparent-data-encryption-tde-overview?tabs=azure-portal), in the [Microsoft.Sql/servers/databases/transparentDataEncryption resource properties](https://docs.microsoft.com/azure/templates/microsoft.sql/servers/databases/transparentdataencryption?tabs=json), add (or update) the value of the *state* property to `enabled`.
#### Severity: 3

### TA-000028: SQL servers with auditing to storage account destination should be configured with 90 days retention or higher
Set the data retention for your SQL Server's auditing to storage account destination to at least 90 days.

**Recommendation**: For incident investigation purposes, we recommend setting the data retention for your SQL Server's auditing to storage account destination to at least 90 days, in the [Microsoft.Sql/servers/auditingSettings resource properties](https://docs.microsoft.com/azure/templates/microsoft.sql/2020-11-01-preview/servers/auditingsettings?tabs=json#serverblobauditingpolicyproperties-object), using the *retentionDays* property. Confirm that you are meeting the necessary retention rules for the regions in which you are operating. This is sometimes required for compliance with regulatory standards.
#### Severity: 3

### TA-000029: Azure API Management APIs should use encrypted protocols only
Set the protocols property to only include HTTPs.

**Recommendation**: To use encrypted protocols only, add (or update) the *protocols* property in the [Microsoft.ApiManagement/service/apis resource properties](https://docs.microsoft.com/azure/templates/microsoft.apimanagement/service/apis?tabs=json), to only include HTTPS. Allowing any additional protocols (e.g. HTTP, WS) is insecure.
#### Severity: 1

### TA-000030: Migrate your Classic Compute VM to ARM
Azure supports two deployment models: classic and Azure Resource Manager (ARM), and ARM provides several security enhancements to the classic model. You can learn more about the differences [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/deployment-models).

**Recommendation**: Use ARM for your virtual machines to provide security enhancements such as: stronger access control (RBAC), better auditing, ARM-based deployment and governance, access to managed identities, access to key vault for secrets, Azure AD-based authentication and support for tags and resource groups for easier security management.
#### Severity: 1

### TA-000031: Migrate your Classic Compute VM to ARM
Azure supports two deployment models: classic and Azure Resource Manager (ARM), and ARM provides several security enhancements to the classic model. You can learn more about the differences [here](https://docs.microsoft.com/en-us/azure/azure-resource-manager/management/deployment-models).

**Recommendation**: Use ARM for your storage accounts to provide security enhancements such as: stronger access control (RBAC), better auditing, ARM-based deployment and governance, access to managed identities, access to key vault for secrets, Azure AD-based authentication and support for tags and resource groups for easier security management.
#### Severity: 1