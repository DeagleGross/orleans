#if NET70
using Orleans.Hosting;
using Orleans.Persistence.Migration;
using Orleans.TestingHost;
using Xunit;

namespace Tester.AzureUtils.Migration
{
    [TestCategory("Functionals"), TestCategory("Migration"), TestCategory("Azure"), TestCategory("AzureBlobStorage")]
    public class MigrationAzureBlobTests : MigrationBaseTests, IClassFixture<MigrationAzureBlobTests.Fixture>
    {
        public static Guid Guid = Guid.NewGuid();

        public class Fixture : BaseAzureTestClusterFixture
        {
            protected override void ConfigureTestCluster(TestClusterBuilder builder)
            {
                builder.AddSiloBuilderConfigurator<SiloConfigurator>();
            }
        }

        private class SiloConfigurator : ISiloConfigurator
        {
            public void Configure(ISiloBuilder siloBuilder)
            {
                siloBuilder
                    .AddMigrationTools()
                    .AddMigrationGrainStorageAsDefault(options =>
                    {
                        options.SourceStorageName = SourceStorageName;
                        options.DestinationStorageName = DestinationStorageName;
                    })
                    .AddAzureBlobGrainStorage(SourceStorageName, options =>
                    {
                        options.ConfigureTestDefaults();
                        options.ContainerName = $"source{Guid}";
                    })
                    .AddMigrationAzureBlobGrainStorage(DestinationStorageName, options =>
                    {
                        options.ConfigureTestDefaults();
                        options.ContainerName = $"destination{Guid}";
                    });
            }
        }

        public MigrationAzureBlobTests(Fixture fixture) : base(fixture)
        {
        }
    }
}
#endif
