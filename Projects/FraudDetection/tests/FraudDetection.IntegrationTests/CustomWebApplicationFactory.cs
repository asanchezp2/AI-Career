using FraudDetection.Api.Health;
using FraudDetection.Application.Abstractions;
using FraudDetection.Infrastructure.Persistence;
using FraudDetection.IntegrationTests.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace FraudDetection.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory that replaces the SQL Server database with a
/// temporary file-based SQLite database for integration testing.
///
/// A file (rather than a shared in-memory connection) is used deliberately:
/// each DbContext then opens its own connection, which lets SQLite's own
/// locking and busy-timeout semantics apply. This matters for concurrency
/// tests where two SaveChanges calls must contend at the database level — a
/// single shared in-memory connection cannot support two concurrent transactions.
///
/// Optionally accepts a configuration callback to override app configuration
/// for tests that exercise startup validation (e.g. invalid KafkaOptions).
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly Action<IConfigurationBuilder>? _configureConfiguration;
    private readonly Action<IServiceCollection>? _configureServices;
    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"FraudDetectionTests-{Guid.NewGuid():N}.db");

    /// <summary>
    /// The IEventPublisher used by the hosts created by this factory. It is a
    /// recording fake (never the real KafkaEventPublisher), so API tests that
    /// POST transactions do not attempt to reach a broker. Tests assert the
    /// published events through this property.
    /// </summary>
    public RecordingEventPublisher EventPublisher { get; } = new();

    /// <summary>
    /// Creates a factory with the default (valid) configuration.
    /// </summary>
    public CustomWebApplicationFactory()
    {
    }

    /// <summary>
    /// Creates a factory that applies the given configuration overrides
    /// on top of appsettings.json.
    /// Internal so the class keeps a single public constructor (required for
    /// xUnit IClassFixture); used by startup-validation tests in this assembly.
    /// </summary>
    /// <param name="configureConfiguration">Callback to append configuration sources.</param>
    internal CustomWebApplicationFactory(Action<IConfigurationBuilder>? configureConfiguration)
    {
        _configureConfiguration = configureConfiguration;
    }

    /// <summary>
    /// Creates a factory that replaces the health check registrations with
    /// the given ones (after the default always-healthy fakes are wired).
    /// Used by tests that exercise the failure path of /health/ready.
    /// The callback lambda body is unambiguous against the
    /// Action&lt;IConfigurationBuilder&gt; constructor above because
    /// IServiceCollection and IConfigurationBuilder share no members used
    /// here (e.g. AddInMemoryCollection only exists on the latter).
    /// </summary>
    /// <param name="configureServices">Callback to mutate the service registrations.</param>
    internal CustomWebApplicationFactory(Action<IServiceCollection>? configureServices)
    {
        _configureServices = configureServices;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Set environment to "Testing" so the Program.cs migration/seeding doesn't run
        builder.UseEnvironment("Testing");

        if (_configureConfiguration is not null)
            builder.ConfigureAppConfiguration(_configureConfiguration);

        builder.ConfigureServices(services =>
        {
            // Remove the SQL Server DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<FraudDetectionDbContext>));

            if (descriptor is not null)
                services.Remove(descriptor);

            // Add SQLite file-based database
            services.AddDbContext<FraudDetectionDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath}"));

            // Ensure the database schema is created
            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<FraudDetectionDbContext>();
            context.Database.EnsureCreated();

            // The API registers KafkaEventPublisher as the IEventPublisher; a
            // produce call would try to reach localhost:9092 with a 10s timeout.
            // Replace it with the recording fake so POST transactions work in
            // the test host and the published events are observable.
            var publisherDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEventPublisher));

            if (publisherDescriptor is not null)
                services.Remove(publisherDescriptor);

            services.AddSingleton<IEventPublisher>(EventPublisher);

            // The API registers real SqlServer + Kafka health checks
            // (ADR-059). The test host has neither SQL Server nor a Kafka
            // broker, so those checks would always report Unhealthy and
            // /health/ready would always return 503. Instead, remove the
            // registrations (they live inside IConfigureOptions
            // &lt;HealthCheckServiceOptions&gt; instances) and re-register
            // always-healthy fakes under the SAME names and "ready" tags —
            // the endpoint contract (200 + checks array with sqlserver and
            // kafka entries) is then fully exercised with deterministic
            // results. Tests that need a failing dependency swap the fakes
            // again through the _configureServices callback.
            services.RemoveAll<IConfigureOptions<HealthCheckServiceOptions>>();
            services.AddHealthChecks()
                .AddCheck(
                    instance: new FakeHealthCheck(),
                    name: HealthCheckNames.SqlServer,
                    tags: new[] { HealthCheckTags.Ready })
                .AddCheck(
                    instance: new FakeHealthCheck(),
                    name: HealthCheckNames.Kafka,
                    tags: new[] { HealthCheckTags.Ready });

            _configureServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Best-effort cleanup of the temp database and its journal files
            foreach (var suffix in new[] { "", "-journal", "-wal", "-shm" })
            {
                try
                {
                    File.Delete(_databasePath + suffix);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
        base.Dispose(disposing);
    }
}