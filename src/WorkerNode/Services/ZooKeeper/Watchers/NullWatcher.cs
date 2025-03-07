using org.apache.zookeeper;

namespace WorkerNode.Services.ZooKeeper.Watchers;

public class NullWatcher : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        return Task.CompletedTask;
    }
}