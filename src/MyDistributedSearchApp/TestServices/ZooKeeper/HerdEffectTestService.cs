using System.Diagnostics;
using MyDistributedSearchApp.Constants;
using MyDistributedSearchApp.Services.ZooKeeper.Interfaces;

namespace MyDistributedSearchApp.TestServices.ZooKeeper;

public class HerdEffectTestService(
    IDistributedLockService lockService,
    ILogger<HerdEffectTestService> logger)
    : BackgroundService
{
    private const string LockName = CommonConstants.TestLockName;
    private const int ParallelAttempts = 50;
    private const int LockAcquiredDelayTimeMs = 5000;
    private const int ExecutionDelayTimeMs = 10000;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("HerdEffectTestService started.");

        while (!cancellationToken.IsCancellationRequested)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < ParallelAttempts; i++)
            {
                var i1 = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var sw = Stopwatch.StartNew();
                        logger.LogInformation("Attempt {Attempt}: Trying to acquire lock '{LockName}'", i1, LockName);
                        var acquired = await lockService.AcquireLockAsync(LockName, cancellationToken);
                        sw.Stop();

                        if (acquired)
                        {
                            logger.LogInformation("Attempt {Attempt}: Lock acquired after {ElapsedMilliseconds} ms", i1,
                                sw.ElapsedMilliseconds);
                            await Task.Delay(LockAcquiredDelayTimeMs, cancellationToken);
                            await lockService.ReleaseLockAsync();
                            logger.LogInformation("Attempt {Attempt}: Lock released", i1);
                        }
                        else
                        {
                            logger.LogInformation("Attempt {Attempt}: Could not acquire lock", i1);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error in parallel lock attempt {Attempt}", i1);
                    }
                }, cancellationToken));
            }

            await Task.WhenAll(tasks);

            await Task.Delay(ExecutionDelayTimeMs, cancellationToken);
        }
    }
}