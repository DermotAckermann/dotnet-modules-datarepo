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


    [Fact, Priority(1)]
    public void Test1()
    {


        Assert.Throws<Exception>(() => );
    }


}


