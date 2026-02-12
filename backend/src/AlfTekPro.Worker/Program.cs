using AlfTekPro.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add worker service
builder.Services.AddHostedService<Worker>();

// Configure Hangfire (will be configured later with actual connection)
// builder.Services.AddHangfire(config =>
// {
//     config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
// });

// Add Hangfire server
// builder.Services.AddHangfireServer();

var host = builder.Build();
host.Run();
