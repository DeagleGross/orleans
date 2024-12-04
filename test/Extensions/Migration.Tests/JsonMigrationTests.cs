using Microsoft.Extensions.DependencyInjection;
using Orleans.Persistence.Migration.Serialization;
using Orleans.TestingHost;
using TestExtensions;
using Xunit;

namespace Migration.Tests
{
    [TestCategory("Functionals"), TestCategory("Migration")]
    public class JsonMigrationTests : HostedTestClusterEnsureDefaultStarted
    {
        private OrleansMigrationJsonSerializer? _target;

        public JsonMigrationTests(MigrationDefaultClusterFixture fixture) : base(fixture)
        {
        }

        private OrleansMigrationJsonSerializer Target
        {
            get
            {
                if (_target == null)
                {
                    var primarySilo = ((InProcessSiloHandle)this.HostedCluster.Primary).SiloHost;
                    _target = primarySilo.Services.GetRequiredService<OrleansMigrationJsonSerializer>();
                }
                return _target;
            }
        }

        [Theory]
        [CsvDataReader("grain-references.csv")]
        public void GrainRefenceSerializerTest(Type grainInterfaceType, string key, string keyExt, string expected)
        {
            var grain = this.GrainFactory.GetTestGrain(grainInterfaceType, key, keyExt);
            var actual = Target.Serialize(grain, grainInterfaceType);
            Assert.Equal(expected, actual);
        }
    }
}