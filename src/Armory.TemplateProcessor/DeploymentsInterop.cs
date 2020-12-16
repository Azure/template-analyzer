// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Azure.Deployments.Core.Instrumentation;
using System.Globalization;

namespace Armory.TemplateProcessor
{
    internal class DeploymentsInterop : IDeploymentsInterop
    {
        private DeploymentsInterop()
        {

        }

        public static void Initialize()
            => Azure.Deployments.Core.Instrumentation.DeploymentsInterop.Initialize(new DeploymentsInterop());

        public CultureInfo GetLocalizationCultureInfo() => CultureInfo.CurrentCulture;
    }
}
