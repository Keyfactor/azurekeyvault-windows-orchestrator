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

