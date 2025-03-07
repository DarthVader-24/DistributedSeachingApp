using org.apache.zookeeper;

namespace MyDistributedSearchApp.Services.ZooKeeper.Watchers;

public class ServiceRegistryWatcher(ILogger logger) : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        logger.LogInformation("ServiceRegistryWatcher triggered: State={State}, Type={Type}, Path={Path}",
            @event.getState(), @event.get_Type(), @event.getPath());
        
        return Task.CompletedTask;
    }
}