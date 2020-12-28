using System.Threading.Tasks;

namespace Armory.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CommandLineParser commandLineParser = new CommandLineParser(args);

            await commandLineParser.InvokeCommandLineAPIAsync().ConfigureAwait(false);
        }

    }
}
