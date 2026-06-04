using Newtonsoft.Json;

namespace NzbDrone.Core.MetadataSource.MetaTube
{
    public class MetaTubeActorResource
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("aliases")]
        public string[] Aliases { get; set; }

        [JsonProperty("images")]
        public string[] Images { get; set; }
    }
}
