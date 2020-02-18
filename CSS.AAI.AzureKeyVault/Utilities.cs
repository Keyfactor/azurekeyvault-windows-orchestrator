using Microsoft.Azure.Management.KeyVault.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CSS.AAI.AzureKeyVault
{
    public static class Utilities
    {
        public const string StoreTypeName = "AKV";

        public class _AKV_JobParameters
        {
            public string VaultURL { get; set; }
            public string TenantId { get; set; }
            public string ClientSecret { get; set; }
            public string ApplicationId { get; set; }
            public string SubscriptionId { get; set; }
            public string VaultName { get; set; }
            public string ResourceGroupName { get; set; }
            public string APIObjectId { get; set; }
            //public string tenantId { get; set; }
        }

        public class _AKV_Location
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("type")]
            public string Type { get; set; }

            [JsonProperty("location")]
            public string location { get; set; }

            [JsonProperty("tags")]
            public _Tags Tags { get; set; }
        }

        public class _DiscoveryResult
        {
            [JsonProperty("value")]
            public List<_AKV_Location> Vaults { get; set; }
        }

        public class _Tags
        {
            List<string> Values { get; set; }
        }

        public class CreateVaultRequest
        {
            [JsonProperty("location")]
            public string Location { get; set; }

            [JsonProperty("properties")]
            public VaultProperties Properties { get; set; }
        }
    }
}
