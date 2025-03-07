using WorkerNode.Options;
using WorkerNode.Services.ZooKeeper;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((hostingContext, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerNodeOptions>(context.Configuration.GetSection("ZooKeeper"));
        services.AddSingleton<WorkerRegistry>();
        services.AddHostedService<WorkerBackgroundService>();
    })
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
    })
    .ConfigureHostOptions(options =>
    {
        options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
    })
    .Build();

await host.RunAsync();