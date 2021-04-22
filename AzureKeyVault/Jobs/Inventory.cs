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
using System.Linq;
using AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Keyfactor.Platform.Extensions.Agents.Interfaces;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    [Job(JobTypes.INVENTORY)]
    public class Inventory : AzureKeyVaultJob, IAgentJobExtension
    {
        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            List<AgentCertStoreInventoryItem> inventoryItems = new List<AgentCertStoreInventoryItem>();

            Initialize(config);

            try
            {
                var certCollection = AzClient.GetCertificatesAsync().Result;

                Logger.Debug($"Found {certCollection.Count()} Total Certificates in Azure Key Vault.");

                foreach (var certificateListing in certCollection)
                {
                    // the items returned by the list only include the thumbprint so we call this for more detail
                    var certificate = AzClient.GetCertificateAsync(certificateListing.Id).Result;

                    inventoryItems.Add(
                        new AgentCertStoreInventoryItem()
                        {
                            Certificates = new string[] { Convert.ToBase64String(certificate.Cer) },
                            Alias = certificate.CertificateIdentifier.Name,
                            PrivateKeyEntry = true, // azure requires private key
                            ItemStatus = AgentInventoryItemStatus.Unknown,
                            UseChainLevel = true
                        }
                    );
                }
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Collection");
            }

            // upload to CMS
            submitInventory.Invoke(inventoryItems);

            return new AnyJobCompleteInfo()
            {
                Status = 2,
                Message = "Inventory Complete"
            };
        }
    }
}

