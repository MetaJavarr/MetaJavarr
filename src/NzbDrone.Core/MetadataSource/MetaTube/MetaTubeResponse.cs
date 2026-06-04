using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.MetaTube
{
    public class MetaTubeResponse<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
