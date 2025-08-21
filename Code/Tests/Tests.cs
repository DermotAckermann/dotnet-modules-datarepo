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
    private static uint entries = 100000;

    public static void RunAll()
    {
        Test1();

        Console.WriteLine("");
    }


    public static void Test1()
    {
        var repo = CreateMultipleKeys(entries);
        WriteMultipleKeys(repo, entries);
        var values = ReadMultipleKeys(repo);
        var all = GetAllKeys(repo);
    }

    public static DataRepo CreateMultipleKeys(uint entries)
    {
        var repo = new DataRepo();

        var keyList = new List<(string, RepoValueType)>();
        for (int i = 0; i < entries; i++)
        {
            string key = $"Test.Key.{i}";
            var type = RepoValueType.String;
            keyList.Add((key, type));
        }

        var sw = Stopwatch.StartNew();
        repo.CreateKeyMulti(keyList);
        sw.Stop();

        Console.WriteLine($"Total Time Create Multi Keys ({entries}): {sw.ElapsedMilliseconds} ms");

        return repo;
    }

    public static void WriteMultipleKeys(DataRepo repo, uint entries)
    {
        var keyValueList = new List<(string, object)>();
        for (int i = 0; i < entries; i++)
        {
            keyValueList.Add(($"Test.Key.{i}", $"Updated {i}"));
        }

        var sw = Stopwatch.StartNew();
        repo.WriteKeyMulti(keyValueList);
        sw.Stop();

        Console.WriteLine($"Total Time Write Multi Keys ({entries}): {sw.ElapsedMilliseconds} ms");
    }

    public static IEnumerable<(string Key, RepoValueBase Value)> GetAllKeys(DataRepo repo)
    {
        var sw = Stopwatch.StartNew();
        var all = repo.GetAll().ToList();
        sw.Stop();

        Console.WriteLine($"Total Time to Get All ({all.Count} entries): {sw.ElapsedMilliseconds} ms");
        return all;
    }

    public static IEnumerable<(string key, object value)> ReadMultipleKeys(DataRepo repo)
    {
        var sw = Stopwatch.StartNew();

        var values = repo.ReadKeyMulti().ToList();

        sw.Stop();

        Console.WriteLine($"Total Time Read Multi Keys ({values.Count}): {sw.ElapsedMilliseconds} ms");

        return values;
    }


}


