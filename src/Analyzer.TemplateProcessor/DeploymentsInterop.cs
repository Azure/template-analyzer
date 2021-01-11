// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Instrumentation = Azure.Deployments.Core.Instrumentation;
using System.Globalization;

namespace Microsoft.Azure.Templates.Analyzer.TemplateProcessor
{
    internal class DeploymentsInterop : Instrumentation.IDeploymentsInterop
    {
        private DeploymentsInterop()
        {

        }

        public static void Initialize()
            => Instrumentation.DeploymentsInterop.Initialize(new DeploymentsInterop());

        public CultureInfo GetLocalizationCultureInfo() => CultureInfo.CurrentCulture;
    }
}
