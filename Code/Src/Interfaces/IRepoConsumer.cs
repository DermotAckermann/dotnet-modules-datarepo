
using AA.Modules.DataRepoModule;

namespace AA.Modules.DataRepoModule;

public interface IRepoConsumer
{
    void InjectRepo(DataRepo repo);
}
