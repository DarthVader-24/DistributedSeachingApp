using Microsoft.Extensions.Options;
using org.apache.zookeeper;
using MyDistributedSearchApp.Options;
using MyDistributedSearchApp.Services.ZooKeeper.Watchers;

namespace MyDistributedSearchApp.Services.ZooKeeper;

public class ServiceRegistry(
    IOptions<ZooKeeperOptions> options,
    ZooKeeperClient client,
    ILogger<ServiceRegistry> logger)
{
    private readonly ZooKeeperOptions _options = options.Value;

    public async Task InitRegistryAsync()
    {
        client.Connect();
        if (client.Zk is null)
        {
            logger.LogError("ZooKeeper connection is null in ServiceRegistry.");
            return;
        }
        await CreatePersistentPathRecursively(_options.ServiceRegistryPath);
    }
        
    private async Task CreatePersistentPathRecursively(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return;

        var lastSlash = path.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var parentPath = path.Substring(0, lastSlash);
            await CreatePersistentPathRecursively(parentPath);
        }

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
        
    public async Task<List<string>> GetActiveWorkersAsync()
    {
        if (client.Zk == null)
        {
            logger.LogError("ZooKeeper connection is null in GetActiveWorkersAsync.");
            return new List<string>();
        }
        var result = await client.Zk.getChildrenAsync(_options.ServiceRegistryPath, new ServiceRegistryWatcher(logger));
        logger.LogInformation("Retrieved {Count} active workers.", result.Children.Count);
        return result.Children;
    }
}