namespace WorkerNode.Services.ZooKeeper;

public class WorkerBackgroundService(WorkerRegistry registry, ILogger<WorkerBackgroundService> logger)
    : BackgroundService
{
    private const int ExecutionDelayTimeMs = 10000;
    private const int WorkerProcessingDelayTimeMs = 3000;
    private const string WorkerIdBase = "1234";
    private const string WorkerMetadataBase = "metadata: CPU=4, Memory=8GB";
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(ExecutionDelayTimeMs, cancellationToken);
        await registry.RegisterWorkerAsync(WorkerIdBase, WorkerMetadataBase);
        logger.LogInformation("WorkerBackgroundService started and worker registered.");

        while (!cancellationToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker processing tasks...");
            await Task.Delay(WorkerProcessingDelayTimeMs, cancellationToken);
        }

        await registry.UnregisterAsync();
        logger.LogInformation("WorkerBackgroundService stopping and worker unregistered.");
    }
}