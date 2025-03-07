

using MyDistributedSearchApp.Constants;
using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;

namespace MyDistributedSearchApp.TestServices.ZooKeeper;

public class LockTestService(IDistributedLockService lockService, ILogger<LockTestService> logger)
    : BackgroundService
{
    private const int LockAcquiredDelayTimeMs = 20000;
    private const int ExecutionDelayTimeMs = 5000;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("LockTestService started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Attempting to acquire lock '{LockName}'...", CommonConstants.TestLockName);
                var lockAcquired = await lockService.AcquireLockAsync(CommonConstants.TestLockName, cancellationToken);
                if (lockAcquired)
                {
                    try
                    {
                        logger.LogInformation("Lock acquired. Executing critical section...");
                        await Task.Delay(LockAcquiredDelayTimeMs, cancellationToken);
                    }
                    finally
                    {
                        await lockService.ReleaseLockAsync();
                        logger.LogInformation("Lock released.");
                    }
                }
                else
                {
                    logger.LogInformation("Could not acquire lock. Will retry after delay.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in LockTestService.");
            }

            await Task.Delay(ExecutionDelayTimeMs, cancellationToken);
        }
    }
}