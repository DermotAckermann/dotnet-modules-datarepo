using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;

namespace AA.Modules.DataRepoModule.Tests;


public static class DataRepoTests
{


    public static void RunAll()
    {
        Test1();

        Console.WriteLine("");
    }


    public static void Test1()
    {
        var repo = CreateTestRepoEntries(100000);
    }

    public static DataRepo CreateTestRepoEntries(int entries)
    {
        var repo = new DataRepo();

        
        var swTotal = Stopwatch.StartNew();
        for (int i = 0; i < entries; i++)
        {
            string key = $"Test.Key.{i}";
            var type = RepoValueType.String;
            string value = $"Value {i}";

            repo.CreateAndWriteKey(key, type, value);

        }

        swTotal.Stop();
        Console.WriteLine($"Total Time CreateAndWriteKey * {entries}: {swTotal.ElapsedMilliseconds} ms");


        swTotal.Restart();
        for (int i = 0; i < entries; i++)
        {
            repo.WriteKey($"Test.Key.{i}", $"Updated {i}");
        }
        swTotal.Stop();
        Console.WriteLine($"Total Time WriteKey * {entries}: {swTotal.ElapsedMilliseconds} ms");

        swTotal.Restart();
        for (int i = 0; i < entries; i++)
        {
            var value = repo.ReadKey($"Test.Key.{i}");
        }
        swTotal.Stop();
        Console.WriteLine($"Total Time ReadKey * {entries}: {swTotal.ElapsedMilliseconds} ms");


        return repo;
    }

}


