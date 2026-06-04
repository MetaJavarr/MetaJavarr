using System;
using System.Text;

namespace NzbDrone.Core.MetadataSource.MetaTube
{
    public static class MetaTubeIdMapper
    {
        public static string ToExternalId(string provider, string id)
        {
            return $"{Normalize(provider)}:{Normalize(id)}";
        }

        public static int ToMetadataId(string provider, string id)
        {
            var identity = ToExternalId(provider, id);

            unchecked
            {
                const uint offset = 2166136261;
                const uint prime = 16777619;
                var hash = offset;

                foreach (var b in Encoding.UTF8.GetBytes(identity))
                {
                    hash ^= b;
                    hash *= prime;
                }

                var metadataId = (int)(hash & 0x7fffffff);

                return metadataId == 0 ? 1 : metadataId;
            }
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("MetaTube identity values must not be empty");
            }

            return value.Trim().ToLowerInvariant();
        }
    }
}
