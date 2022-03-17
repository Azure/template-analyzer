// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Globalization;
using Azure.Deployments.Core.Instrumentation;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor
{
    internal class AnalyzerDeploymentsInterop : IDeploymentsInterop
    {
        private AnalyzerDeploymentsInterop()
        {

        }

        int IDeploymentsInterop.DeploymentNameLengthLimit => throw new System.NotImplementedException();

        int IDeploymentsInterop.DeploymentKeyVaultReferenceLimit => throw new System.NotImplementedException();

        int IDeploymentsInterop.DeploymentResourceGroupLimit => throw new System.NotImplementedException();

        bool IDeploymentsInterop.KeyVaultDeploymentEnabled => throw new System.NotImplementedException();

        public static void Initialize()
            => DeploymentsInterop.Initialize(new AnalyzerDeploymentsInterop());

        public CultureInfo GetLocalizationCultureInfo() => CultureInfo.CurrentCulture;
    }
}
