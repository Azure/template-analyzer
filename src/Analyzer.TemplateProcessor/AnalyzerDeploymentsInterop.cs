// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using Azure.Deployments.Core.Instrumentation;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor
{
    internal class AnalyzerDeploymentsInterop : IDeploymentsInterop
    {
        private AnalyzerDeploymentsInterop()
        {

        }

        public int DeploymentNameLengthLimit => throw new NotImplementedException();

        public static void Initialize()
            => DeploymentsInterop.Initialize(new AnalyzerDeploymentsInterop());

        public CultureInfo GetLocalizationCultureInfo() => CultureInfo.CurrentCulture;
    }
}
