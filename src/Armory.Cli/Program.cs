// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace Armory.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandLineParser commandLineParser = new CommandLineParser();

            await commandLineParser.InvokeCommandLineAPIAsync(args).ConfigureAwait(false);
        }

    }
}
