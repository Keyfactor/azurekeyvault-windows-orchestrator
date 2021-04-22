// Copyright 2021 Keyfactor
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Newtonsoft.Json;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    [Job(JobTypes.DISCOVERY)]
    public class Discovery : AzureKeyVaultJob, IAgentJobExtension
    {
        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            var keyVaults = new List<string>();

            Initialize(config);

            try
            {
                var result = AzClient.GetVaults().Result;
                foreach (var keyVault in result.Vaults)
                    keyVaults.Add(keyVault.Name);
            }
            catch (Exception ex)
            {
                return ThrowError(ex, "List Vaults");
            }

            sdr.Invoke(keyVaults);

            return new AnyJobCompleteInfo()
            {
                Status = 2,
                Message = "Discovery Complete"
            };
        }
    }

    public class _DiscoveryResult
    {
        [JsonProperty("value")]
        public List<_AKV_Location> Vaults { get; set; }
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

    public class _Tags
    {
        public List<string> Values { get; set; }
    }
}
