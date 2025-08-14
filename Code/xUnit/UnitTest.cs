using System.Text.Json;
using Xunit.Priority;
using AA.Modules.DataRepoModule;


namespace AA.Modules.DataRepoModule.TestsXUnit;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class DataRepoTests
{

    static DataRepoTests()
    {
        
    }


    [Fact]
    public void DR001_Initialize_EmptyRepository()
    {
        var repo = new DataRepo();
        Assert.NotNull(repo);
        // On init, no keys exist; structure timestamp should be near now
        Assert.True((DateTime.UtcNow - repo.LastStructureChangeUtc).TotalMinutes < 5);
    }

    [Fact]
    public void DR002_Create_IntegerKey()
    {
        var repo = new DataRepo();
        repo.CreateKey("speed", RepoValueType.Integer);
        var keys = repo.FindKeys("speed").ToList();
        Assert.Contains("speed", keys);
    }

    [Fact]
    public void DR003_Write_IntegerValue()
    {
        var repo = new DataRepo();
        repo.CreateKey("speed", RepoValueType.Integer);
        repo.WriteKey("speed", 120);
        Assert.Equal("120", repo.ReadKeyString("speed"));
    }

    [Fact]
    public void DR004_Read_IntegerValue()
    {
        var repo = new DataRepo();
        repo.CreateKey("speed", RepoValueType.Integer);
        repo.WriteKey("speed", 88);
        var rv = repo.ReadKey("speed");
        Assert.NotNull(rv);
        Assert.Equal("88", rv.GetValue()?.ToString());
    }

    [Fact]
    public void DR005_TypeEnforcement_Mismatch_IsRejected()
    {
        var repo = new DataRepo();
        repo.CreateKey("speed", RepoValueType.Integer, typeEnforcement: true);
        Assert.Throws<ArgumentException>(() => repo.WriteKey("speed", "fast"));
        repo.WriteKey("speed", 120);
        Assert.Equal("120", repo.ReadKeyString("speed"));
        Assert.Throws<ArgumentException>(() => repo.WriteKey("speed", "zzz"));
        Assert.Equal("120", repo.ReadKeyString("speed"));
    }

    [Fact]
    public void DR006_NoTypeEnforcement_AllowsConvertibleWrite()
    {
        var repo = new DataRepo();
        repo.CreateKey("speed", RepoValueType.Integer, typeEnforcement: false);
        repo.WriteKey("speed", "130");
        Assert.Equal("130", repo.ReadKeyString("speed"));
        repo.WriteKey("speed", 130.0);
        Assert.Equal("130", repo.ReadKeyString("speed"));
    }

    [Fact]
    public void DR007_Create_StringKey()
    {
        var repo = new DataRepo();
        repo.CreateKey("mode", RepoValueType.String);
        var keys = repo.FindKeys("mode").ToList();
        Assert.Contains("mode", keys);
    }

    [Fact]
    public void DR008_WriteRead_String()
    {
        var repo = new DataRepo();
        repo.CreateKey("mode", RepoValueType.String);
        repo.WriteKey("mode", "auto");
        Assert.Equal("auto", repo.ReadKeyString("mode"));
    }

    [Fact]
    public void DR009_Create_Json_And_WriteObject()
    {
        var repo = new DataRepo();
        repo.CreateKey("cfg", RepoValueType.Json);
        repo.WriteKey("cfg", "{\"a\":1,\"b\":\"x\"}");
        var json = repo.ReadKeyString("cfg");
        Assert.Contains("\"a\":1", json.Replace(" ", ""));
        Assert.Contains("\"b\":\"x\"", json.Replace(" ", ""));
    }

    [Fact]
    public void DR010_Timestamp_Utc_WriteRead()
    {
        var repo = new DataRepo();
        repo.CreateKey("lastRun", RepoValueType.TimestampUtc);
        var now = DateTime.UtcNow;
        repo.WriteKey("lastRun", now);
        var rv = repo.ReadKey("lastRun");
        Assert.NotNull(rv);
        var str = repo.ReadKeyString("lastRun");
        Assert.False(string.IsNullOrWhiteSpace(str));
        Assert.Equal(now.ToString(), str);
    }

    [Fact]
    public void DR011_Boolean_WriteRead()
    {
        var repo = new DataRepo();
        repo.CreateKey("enabled", RepoValueType.Bool);
        repo.WriteKey("enabled", true);
        Assert.Equal("True", repo.ReadKeyString("enabled"));
    }

    [Fact]
    public void DR012_Float_WriteRead()
    {
        var repo = new DataRepo();
        repo.CreateKey("ratio", RepoValueType.Float);
        repo.WriteKey("ratio", 0.75f);
        var s = repo.ReadKeyString("ratio");
        Assert.StartsWith("0,75", s, StringComparison.OrdinalIgnoreCase);//debug
    }

    [Fact]
    public void DR013_CreateAndWriteKey_Works()
    {
        var repo = new DataRepo();
        repo.CreateAndWriteKey("temp", RepoValueType.Float, 23.5);
        Assert.StartsWith("23,5", repo.ReadKeyString("temp"));
    }

    [Fact]
    public void DR014_DuplicateKeyCreation_Fails()
    {
        var repo = new DataRepo();
        repo.CreateKey("mode", RepoValueType.String);
        Assert.ThrowsAny<Exception>(() => repo.CreateKey("mode", RepoValueType.String));
    }

    [Fact]
    public void DR015_ReadMissingKey_Fails()
    {
        var repo = new DataRepo();
        Assert.ThrowsAny<Exception>(() => repo.ReadKey("unknown"));
    }

    [Fact]
    public void DR016_WritePassword_Enforced()
    {
        var repo = new DataRepo();
        // create with write password
        repo.CreateKey("secure", RepoValueType.String, passWrite: "w123");
        Assert.ThrowsAny<Exception>(() => repo.WriteKey("secure", "x")); // no pass
        repo.WriteKey("secure", "x", writePass:"w123");
        Assert.Equal("x", repo.ReadKeyString("secure"));
    }

    [Fact]
    public void DR017_ReadPassword_Enforced()
    {
        var repo = new DataRepo();
        repo.CreateKey("secret", RepoValueType.String, passRead: "r123", passWrite: "w123");
        repo.WriteKey("secret", "top", writePass: "w123");
        Assert.ThrowsAny<Exception>(() => repo.ReadKey("secret")); // missing read pass
        var value = repo.ReadKey("secret", pass: "r123");
        Assert.Equal("top", value.GetValue()?.ToString());
    }

    [Fact]
    public void DR018_DeletePassword_Enforced()
    {
        var repo = new DataRepo();
        repo.CreateAndWriteKey("tempKey", RepoValueType.Integer, 7, deleteAndWritePass: "d123");
        Assert.ThrowsAny<Exception>(() => repo.DeleteKey("tempKey"));
        repo.DeleteKey("tempKey", deletePass: "d123");
        Assert.ThrowsAny<Exception>(() => repo.ReadKey("tempKey"));
    }

    [Fact]
    public void DR019_BulkCreate_Works()
    {
        var repo = new DataRepo();
        repo.CreateKeyMulti(new (string key, RepoValueType type)[]
        {
                ("a.x", RepoValueType.Integer),
                ("a.y", RepoValueType.String),
                ("a.z", RepoValueType.Bool)
        });
        var keys = repo.FindKeys("a").ToList();
        Assert.Contains("a.x", keys);
        Assert.Contains("a.y", keys);
        Assert.Contains("a.z", keys);
    }

    /*
    [Fact]
    public void DR020_AddArrayElement_Works()
    {
        var repo = new DataRepo();
        // The array key template must end with ".[]"
        var id0 = repo.AddArrayElement("orders", RepoValueType.Integer, 10);
        var id1 = repo.AddArrayElement("orders.[].", RepoValueType.Integer, 11);
        Assert.Equal(2, repo.GetArrayIndexCount("orders.[]"));
        Assert.Equal("10", repo.ReadKeyString($"orders[{id0}]"));
        Assert.Equal("11", repo.ReadKeyString($"orders[{id1}]"));
    }
    */

    [Fact]
    public void DR021_SetKeyNull_ClearsValue()
    {
        var repo = new DataRepo();
        repo.CreateAndWriteKey("mode", RepoValueType.String, "auto");
        repo.SetKeyNull("mode");
        Assert.Equal(string.Empty, repo.ReadKeyString("mode"));
    }

    [Fact]
    public void DR022_LastStructureChangeUtc_Updates_On_CreateDelete()
    {
        var repo = new DataRepo();
        var t0 = repo.LastStructureChangeUtc;
        repo.CreateKey("new", RepoValueType.String);
        var t1 = repo.LastStructureChangeUtc;
        repo.DeleteKey("new");
        var t2 = repo.LastStructureChangeUtc;

        Assert.True(t1 >= t0);
        Assert.True(t2 >= t1);
    }

    [Theory]
    [InlineData("base", "id", true, "base.{id}.")]
    [InlineData("base", "id", false, "base.{id}")]
    public void DR023_AppendKeyEntityId_Works(string baseKey, string id, bool addDot, string expected)
    {
        var k = DataRepo.AppendKeyEntityId(baseKey, id, addDot);
        Assert.Equal(expected, k);
    }

    [Theory]
    [InlineData("base", 3, true, "base.[3].")]
    [InlineData("base", 0, false, "base.[0]")]
    public void DR024_AppendKeyArrayIndex_Works(string baseKey, int idx, bool addDot, string expected)
    {
        var k = DataRepo.AppendKeyArrayIndex(baseKey, idx, addDot);
        Assert.Equal(expected, k);
    }

    [Fact]
    public void DR025_KeyName_CaseSensitivity()
    {
        var repo = new DataRepo();
        repo.CreateKey("Mode", RepoValueType.String);
        repo.WriteKey("Mode", "auto");
        Assert.ThrowsAny<Exception>(() => repo.ReadKey("mode"));
    }

    [Fact]
    public void DR026_Write_Overwrites_Value()
    {
        var repo = new DataRepo();
        repo.CreateKey("mode", RepoValueType.String);
        repo.WriteKey("mode", "auto");
        repo.WriteKey("mode", "manual");
        Assert.Equal("manual", repo.ReadKeyString("mode"));
    }

    [Fact]
    public void DR027_Null_And_Empty_String_Handling()
    {
        var repo = new DataRepo();
        repo.CreateKey("name", RepoValueType.String);
        repo.WriteKey("name", null);
        Assert.Equal(string.Empty, repo.ReadKeyString("name"));
        repo.WriteKey("name", "");
        Assert.Equal(string.Empty, repo.ReadKeyString("name"));
    }

    [Fact]
    public void DR028_DeleteAll_With_DeleteKeyMulti()
    {
        var repo = new DataRepo();
        repo.CreateKey("a", RepoValueType.Integer);
        repo.CreateKey("b", RepoValueType.String);
        repo.CreateKey("c", RepoValueType.Bool);
        repo.WriteKey("a", 1);
        repo.WriteKey("b", "x");
        repo.WriteKey("c", true);
        var t0 = repo.LastStructureChangeUtc;
        var keys = repo.GetAll().Select(entry => entry.Key).ToList();
        Assert.NotNull(keys);
        repo.DeleteKeyMulti(keys);
        var remaining = repo.GetAll().Select(entry => entry.Key).ToList();
        Assert.Empty(remaining);
        Assert.True(repo.LastStructureChangeUtc >= t0);
    }

    [Fact]
    public void DR029_CreateFromJson_BuildsKeysAndValues()
    {
        var repo = new DataRepo();
        string json = "{ \"cfg\": { \"a\": 1, \"b\": \"x\" }, \"enabled\": true }";
        repo.CreateFromJson(json);
        var keys = repo.GetAll().Select(entry => entry.Key).ToList();
        Assert.Contains("cfg.a", keys);
        Assert.Contains("cfg.b", keys);
        Assert.Contains("enabled", keys);

        Assert.Equal("1", repo.ReadKeyString("cfg.a"));
        Assert.Equal("x", repo.ReadKeyString("cfg.b"));
        Assert.Equal("True", repo.ReadKeyString("enabled"));
    }

    private class Consumer : IRepoConsumer
    {
        public DataRepo? Repo { get; private set; }
        public void InjectRepo(DataRepo repo) => Repo = repo;
    }

    [Fact]
    public void DR030_IRepoConsumer_Injects_Repo_Instance()
    {
        var consumer = new Consumer();
        var repo = new DataRepo();
        repo.CreateAndWriteKey("greeting", RepoValueType.String, "hi");
        consumer.InjectRepo(repo);
        Assert.NotNull(consumer.Repo);
        Assert.Equal("hi", consumer.Repo!.ReadKeyString("greeting"));
    }

    [Fact]
    public void WriteKeyMulti_Writes_All_Values()
    {
        var repo = new DataRepo();
        repo.CreateKey("a", RepoValueType.Integer);
        repo.CreateKey("b", RepoValueType.String);
        repo.CreateKey("c", RepoValueType.Bool);

        repo.WriteKeyMulti(new (string key, object value)[]
        {
                ("a", 10),
                ("b", "hello"),
                ("c", true)
        });

        Assert.Equal("10", repo.ReadKeyString("a"));
        Assert.Equal("hello", repo.ReadKeyString("b"));
        Assert.Equal("True", repo.ReadKeyString("c"));
    }

    [Fact]
    public void WriteKeyMulti_Respects_Write_Password()
    {
        var repo = new DataRepo();
        // create protected keys
        repo.CreateKey("p1", RepoValueType.Integer, passWrite: "w");
        repo.CreateKey("p2", RepoValueType.Integer, passWrite: "w");

        // wrong password => should throw on first attempt
        Assert.Throws<UnauthorizedAccessException>(() =>
            repo.WriteKeyMulti(new (string key, object value)[] { ("p1", 1), ("p2", 2) }, writePass: "bad"));

        // correct password => succeeds
        repo.WriteKeyMulti(new (string key, object value)[] { ("p1", 1), ("p2", 2) }, writePass: "w");
        Assert.Equal("1", repo.ReadKeyString("p1"));
        Assert.Equal("2", repo.ReadKeyString("p2"));
    }

    [Fact]
    public void SetKeyNullMulti_Clears_All_Specified_Keys()
    {
        var repo = new DataRepo();
        repo.CreateAndWriteKey("k1", RepoValueType.String, "v1");
        repo.CreateAndWriteKey("k2", RepoValueType.String, "v2");
        repo.CreateAndWriteKey("k3", RepoValueType.String, "v3");

        repo.SetKeyNullMulti(new[] { "k1", "k3" });

        Assert.Equal(string.Empty, repo.ReadKeyString("k1"));
        Assert.Equal("v2", repo.ReadKeyString("k2")); // untouched
        Assert.Equal(string.Empty, repo.ReadKeyString("k3"));
    }

    [Fact]
    public void FindKeysWithWildcards_Basic_And_Nested()
    {
        var repo = new DataRepo();
        repo.CreateKey("cfg.a", RepoValueType.Integer);
        repo.CreateKey("cfg.b", RepoValueType.Integer);
        repo.CreateKey("cfg.detail.value", RepoValueType.Integer);
        repo.CreateKey("other", RepoValueType.Integer);

        var allCfg = repo.FindKeysWithWildcards("cfg.*").ToList();
        Assert.Contains("cfg.a", allCfg);
        Assert.Contains("cfg.b", allCfg);
        Assert.Contains("cfg.detail.value", allCfg);
        Assert.DoesNotContain("other", allCfg);

        var nested = repo.FindKeysWithWildcards("cfg.*.value").ToList();
        Assert.Single(nested);
        Assert.Equal("cfg.detail.value", nested[0]);

        Assert.Throws<ArgumentException>(() => repo.FindKeysWithWildcards("").ToList());
    }

    [Fact]
    public void GetEntityIds_Matches_Keys_Built_With_AppendKeyEntityId()
    {
        var repo = new DataRepo();
        // Build keys like: devices.{A1}.status
        var baseKey = "devices";
        var kA = DataRepo.AppendKeyEntityId(baseKey, "A1", addDot: true) + "status";
        var kB = DataRepo.AppendKeyEntityId(baseKey, "B2", addDot: true) + "status";

        repo.CreateKey(kA, RepoValueType.String);
        repo.CreateKey(kB, RepoValueType.String);

        var ids = repo.GetEntityIds(baseKey).OrderBy(x => x).ToList();
        Assert.Equal(new[] { "A1", "B2" }, ids);
    }


}


