using System.Text;
using Microsoft.Extensions.Options;
using org.apache.zookeeper;
using WorkerNode.Constants;
using WorkerNode.Options;
using WorkerNode.Services.ZooKeeper.Watchers;

namespace WorkerNode.Services.ZooKeeper;

public class WorkerRegistry(IOptions<WorkerNodeOptions> options, ILogger<WorkerRegistry> logger)
{
    private readonly WorkerNodeOptions _options = options.Value;
    private org.apache.zookeeper.ZooKeeper? _zk;
    private string _workerNodePath = string.Empty;

    private void Connect()
    {
        if (_zk is not null) return;

        _zk = new org.apache.zookeeper.ZooKeeper(
            _options.ConnectionString,
            _options.SessionTimeoutMs,
            new NullWatcher());

        logger.LogInformation("Connecting to ZooKeeper: {ConnectionString}", _options.ConnectionString);
    }

    public async Task RegisterWorkerAsync(string workerId, string metadata)
    {
        Connect();

        if (_zk is null)
            throw new Exception("ZooKeeper is not connected.");

        await CreatePersistentPathRecursivelyAsync(_options.ServiceRegistryPath);

        _workerNodePath = $"{_options.ServiceRegistryPath}{CommonConstants.WorkerPathPrefix}{workerId}";

        var exists = await _zk.existsAsync(_workerNodePath);
        if (exists != null)
        {
            logger.LogInformation("Worker already registered at: {WorkerNodePath}", _workerNodePath);
            return;
        }

        var createdPath = await RetryAsync(async () => await _zk.createAsync(
            _workerNodePath,
            Encoding.UTF8.GetBytes(metadata),
            ZooDefs.Ids.OPEN_ACL_UNSAFE,
            CreateMode.EPHEMERAL));

        _workerNodePath = createdPath;
        logger.LogInformation("Worker registered at path: {WorkerNodePath}", _workerNodePath);
    }

    public async Task UnregisterAsync()
    {
        if (_zk is null)
            return;

        var stat = await _zk.existsAsync(_workerNodePath);

        if (stat is not null)
        {
            await _zk.deleteAsync(_workerNodePath, stat.getVersion());
        }

        logger.LogInformation("Worker unregistered from path: {WorkerNodePath}", _workerNodePath);
    }

    private async Task CreatePersistentPathRecursivelyAsync(string path)
    {
        if (string.IsNullOrEmpty(path) || path == "/") return;

        var lastSlash = path.LastIndexOf('/');
        if (lastSlash > 0)
        {
            var parentPath = path.Substring(0, lastSlash);
            await CreatePersistentPathRecursivelyAsync(parentPath);
        }

        var stat = await _zk!.existsAsync(path);
        if (stat is null)
        {
            try
            {
                await _zk.createAsync(path, null, ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                logger.LogInformation("Persistent path created: {Path}", path);
            }
            catch (KeeperException.NodeExistsException)
            {
                logger.LogInformation("Persistent path already exists: {Path}", path);
            }
        }
    }

    private async Task<T> RetryAsync<T>(Func<Task<T>> operation, int retryCount = CommonConstants.ConnectionMaxRetries,
        int delayMilliseconds = CommonConstants.ConnectionDelayTimeMs)
    {
        for (var attempt = 1; attempt <= retryCount; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (KeeperException.ConnectionLossException ex)
            {
                logger.LogWarning("Connection loss on attempt {Attempt}: {Message}. Retrying in {Delay} ms", attempt,
                    ex.Message, delayMilliseconds);
                await Task.Delay(delayMilliseconds);
            }
            catch (KeeperException.NoNodeException ex)
            {
                logger.LogWarning("NoNodeException on attempt {Attempt}: {Message}. Retrying in {Delay} ms", attempt,
                    ex.Message, delayMilliseconds);
                await Task.Delay(delayMilliseconds);
            }
        }

        throw new Exception("Operation failed after retries");
    }
}