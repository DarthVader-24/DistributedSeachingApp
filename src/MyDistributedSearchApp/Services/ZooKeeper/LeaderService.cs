using Microsoft.Extensions.Options;
using MyDistributedSearchApp.Constants;
using org.apache.zookeeper;
using MyDistributedSearchApp.Options;
using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;
using MyDistributedSearchApp.Services.ZooKeeper.Watchers;

namespace MyDistributedSearchApp.Services.ZooKeeper;

public class LeaderService(ZooKeeperClient client, IOptions<ZooKeeperOptions> options, ILogger<LeaderService> logger)
    : ILeaderService
{
    private readonly ZooKeeperOptions _options = options.Value;
    private string? _nodePath;

    public async Task StartElection()
    {
        client.Connect();

        await CreatePersistentPathIfNotExistsWithRetry(_options.LeaderElectionPath);

        try
        {
            _nodePath = await CreateEphemeralSequentialWithRetry(_options.LeaderElectionPath +
                                                                 CommonConstants.NodePathPrefix);
            logger.LogInformation("Created ephemeral sequential node: {NodePath}", _nodePath);

            await DetermineLeader();

            await WatchLeaderElection();
        }
        catch (KeeperException.NodeExistsException)
        {
            logger.LogInformation("Sequential node already exists: {NodePath}", _nodePath);
        }
    }

    public async Task DetermineLeader()
    {
        var childrenResult = await client.Zk!.getChildrenAsync(_options.LeaderElectionPath);
        if (childrenResult is null || _nodePath is null) return;

        childrenResult.Children.Sort();
        var myNodeName = _nodePath[(_nodePath.LastIndexOf('/') + 1)..];

        if (childrenResult.Children.Count > 0 && childrenResult.Children[0] == myNodeName)
        {
            logger.LogInformation("I am the LEADER! (My node: {NodePath})", _nodePath);
        }
        else
        {
            logger.LogInformation("I am a FOLLOWER. (My node: {NodePath})", _nodePath);
        }
    }

    private async Task CreatePersistentPathIfNotExistsWithRetry(string path)
    {
        for (var attempt = 1; attempt <= CommonConstants.ConnectionMaxRetries; attempt++)
        {
            try
            {
                var stat = await client.Zk!.existsAsync(path);
                if (stat is not null) return;
                await client.Zk.createAsync(path, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                logger.LogInformation("Persistent path created: {Path}", path);
                return;
            }
            catch (KeeperException.ConnectionLossException ex)
            {
                logger.LogWarning(
                    "Connection loss when creating persistent path '{Path}', attempt {Attempt}: {Message}", path,
                    attempt, ex.Message);
                await Task.Delay(CommonConstants.ConnectionDelayTimeMs);
            }
            catch (KeeperException.NodeExistsException)
            {
                logger.LogInformation("Persistent path already exists: {0}", path);
                return;
            }
        }

        throw new Exception(
            $"Failed to create persistent path '{path}' after {CommonConstants.ConnectionMaxRetries} attempts.");
    }

    private async Task<string> CreateEphemeralSequentialWithRetry(string pathPrefix)
    {
        for (var attempt = 1; attempt <= CommonConstants.ConnectionMaxRetries; attempt++)
        {
            try
            {
                var path = await client.Zk!.createAsync(pathPrefix, null, ZooDefs.Ids.OPEN_ACL_UNSAFE,
                    CreateMode.EPHEMERAL_SEQUENTIAL);
                return path;
            }
            catch (KeeperException.ConnectionLossException ex)
            {
                logger.LogWarning(
                    "Connection loss when creating ephemeral sequential node with prefix '{PathPrefix}', attempt {Attempt}: {Message}",
                    pathPrefix, attempt, ex.Message);
                await Task.Delay(CommonConstants.ConnectionDelayTimeMs);
            }
        }

        throw new Exception(
            $"Failed to create ephemeral sequential node with prefix '{pathPrefix}' after {CommonConstants.ConnectionMaxRetries} attempts.");
    }

    public async Task WatchLeaderElection() =>
        await client.Zk!.getChildrenAsync(_options.LeaderElectionPath, new LeaderElectionWatcher(this, logger));
}