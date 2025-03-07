using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;
using org.apache.zookeeper;

namespace MyDistributedSearchApp.Services.ZooKeeper.Watchers;

public class LeaderElectionWatcher(ILeaderService leaderService, ILogger logger) : Watcher
{
    public override async Task process(WatchedEvent @event)
    {
        logger.LogInformation("LeaderElectionWatcher triggered: State={State}, Type={Type}, Path={Path}",
            @event.getState(), @event.get_Type(), @event.getPath());

        await leaderService.DetermineLeader();

        await leaderService.WatchLeaderElection();
    }
}