using MyDistributedSearchApp.Constants;
using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;
using MyDistributedSearchApp.Services.ZooKeeper.Watchers;
using org.apache.zookeeper;

namespace MyDistributedSearchApp.Services.ZooKeeper;

public class DistributedLockService(
    ZooKeeperClient client,
    ILogger<DistributedLockService> logger) : IDistributedLockService
{
    private string? _lockNodePath;

    public async Task<bool> AcquireLockAsync(string lockName, CancellationToken cancellationToken)
    {
        client.Connect();

        await CreatePersistentPathIfNotExistsAsync(CommonConstants.LockBasePath);
        var fullLockPath = $"{CommonConstants.LockBasePath}/{lockName}";
        await CreatePersistentPathIfNotExistsAsync(fullLockPath);

        var pathPrefix = fullLockPath + CommonConstants.LockPathPrefix;
        _lockNodePath = await client.Zk!.createAsync(pathPrefix, null, ZooDefs.Ids.OPEN_ACL_UNSAFE,
            CreateMode.EPHEMERAL_SEQUENTIAL);
        logger.LogInformation("Created ephemeral sequential lock node: {LockNodePath}", _lockNodePath);

        await TryAcquireOrWatchAsync(fullLockPath, cancellationToken);

        return true;
    }

    public async Task ReleaseLockAsync()
    {
        if (_lockNodePath is null)
            return;

        try
        {
            var stat = await client.Zk!.existsAsync(_lockNodePath);
            if (stat != null)
            {
                await client.Zk.deleteAsync(_lockNodePath, stat.getVersion());
                logger.LogInformation("Released lock by deleting znode: {LockNodePath}", _lockNodePath);
                _lockNodePath = null;
            }
        }
        catch (KeeperException ex)
        {
            logger.LogWarning("Failed to release lock: {Message}", ex.Message);
        }
    }

    private async Task TryAcquireOrWatchAsync(string lockPath, CancellationToken cancellationToken)
    {
        var childrenResult = await client.Zk!.getChildrenAsync(lockPath);
        if (childrenResult is null || _lockNodePath is null)
            return;

        childrenResult.Children.Sort();
        var myNodeName = _lockNodePath.Substring(_lockNodePath.LastIndexOf('/') + 1);

        var index = childrenResult.Children.IndexOf(myNodeName);
        switch (index)
        {
            case -1:
                logger.LogError("Our lock node not found in {LockPath} children. Something is wrong!", lockPath);
                return;
            case 0:
                logger.LogInformation("We acquired the lock! (My node: {NodeName})", myNodeName);
                break;
            default:
            {
                var prevNodeName = childrenResult.Children[index - 1];
                var prevNodePath = lockPath + "/" + prevNodeName;

                logger.LogInformation("We do NOT hold the lock. Will watch node before us: {PrevNodePath}", prevNodePath);

                var stat = await client.Zk.existsAsync(prevNodePath,
                    new LockWatcher(this, prevNodePath, lockPath, cancellationToken));
                if (stat == null)
                {
                    logger.LogInformation("Previous node already gone, re-checking lock...");
                    await TryAcquireOrWatchAsync(lockPath, cancellationToken);
                }

                break;
            }
        }
    }

    public async Task OnLockNodeDeleted(string deletedPath, string lockPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Watcher triggered: node {DeletedPath} was deleted, re-checking lock...", deletedPath);
        await TryAcquireOrWatchAsync(lockPath, cancellationToken);
    }

    private async Task CreatePersistentPathIfNotExistsAsync(string path)
    {
        var stat = await client.Zk!.existsAsync(path);
        if (stat is null)
        {
            try
            {
                await client.Zk.createAsync(path, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                logger.LogInformation("Persistent path created: {Path}", path);
            }
            catch (KeeperException.NodeExistsException)
            {
                logger.LogInformation("Persistent path already exists: {Path}", path);
            }
        }
    }
}