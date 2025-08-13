

using AA.Modules.DataRepoModule.Tests;
using AA.Modules.DataRepoModule;

namespace EcMasterAcontisApp;

internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting  Test...");

        DataRepoTests.RunAll();



        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();

    }
}