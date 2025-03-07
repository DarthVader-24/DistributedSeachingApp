using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;
using org.apache.zookeeper;

namespace MyDistributedSearchApp.Services.ZooKeeper.Watchers;

public class LockWatcher(
    IDistributedLockService lockService,
    string watchedPath,
    string lockPath,
    CancellationToken cancellationToken)
    : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        if (@event.get_Type() == Event.EventType.NodeDeleted &&
            @event.getPath() == watchedPath)
        {
            return lockService.OnLockNodeDeleted(watchedPath, lockPath, cancellationToken);
        }

        return Task.CompletedTask;
    }
}