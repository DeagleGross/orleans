using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans.Runtime;
using Orleans.Storage;

namespace Orleans.Persistence.Migration
{
    public class MigrationGrainStorage : IGrainStorage
    {
        [Serializable]
        private struct MigrationEtag
        {
            public string SourceETag { get; set; }
            public string DestinationETag { get; set; }

            public MigrationEtag(string sourceETag, string destinationETag)
            {
                this.SourceETag = sourceETag;
                this.DestinationETag = destinationETag;
            }

            public string SerializeToJson() => JsonConvert.SerializeObject(this, typeof(MigrationEtag), null);

            public static string SerializeToJson(string sourceETag, string destinationETag)
            {
                var obj = new MigrationEtag(sourceETag, destinationETag);
                return JsonConvert.SerializeObject(obj);
            }

            public static MigrationEtag ParseFromJson(string json) => (MigrationEtag) JsonConvert.DeserializeObject(json, typeof(MigrationEtag));
        }

        private readonly IGrainStorage _sourceStorage;
        private readonly IGrainStorage _destinationStorage;

        public MigrationGrainStorage(IGrainStorage sourceStorage, IGrainStorage destinationStorage)
        {
            this._sourceStorage = sourceStorage;
            this._destinationStorage = destinationStorage;
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var eTag = MigrationEtag.ParseFromJson(grainState.ETag);
            grainState.ETag = eTag.SourceETag;
            await _sourceStorage.ClearStateAsync(grainType, grainReference, grainState);
            grainState.ETag = eTag.DestinationETag;
            await _destinationStorage.ClearStateAsync(grainType, grainReference, grainState);
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            await _destinationStorage.ReadStateAsync(grainType, grainReference, grainState);
            if (grainState.RecordExists)
            {
                grainState.ETag = MigrationEtag.SerializeToJson(null, grainState.ETag);
            }
            else
            {
                await _sourceStorage.ReadStateAsync(grainType, grainReference, grainState);
                grainState.ETag = MigrationEtag.SerializeToJson(grainState.ETag, null);
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            MigrationEtag etag = new MigrationEtag();
            if (grainState.RecordExists)
            {
                var eTag = MigrationEtag.ParseFromJson(grainState.ETag);
                grainState.ETag = eTag.DestinationETag;
            }
            try
            {
                await _destinationStorage.WriteStateAsync(grainType, grainReference, grainState);
                etag.DestinationETag = grainState.ETag;
            }
            finally
            {
                grainState.ETag = etag.SerializeToJson();
            }
        }

        public IAsyncEnumerable<StorageEntry> GetAll(CancellationToken cancellationToken) => throw new NotImplementedException();

        public static IGrainStorage Create(IServiceProvider serviceProvider, string name)
        {
            var options = serviceProvider
                .GetRequiredService<IOptionsMonitor<MigrationGrainStorageOptions>>()
                .Get(name);
            var source = serviceProvider.GetRequiredServiceByName<IGrainStorage>(options.SourceStorageName);
            var destination = serviceProvider.GetRequiredServiceByName<IGrainStorage>(options.DestinationStorageName);
            return new MigrationGrainStorage(source, destination);
        }
    }

    public class MigrationGrainStorageOptions
    {
        public string SourceStorageName { get; set; }

        public string DestinationStorageName { get; set; }
    }
}
