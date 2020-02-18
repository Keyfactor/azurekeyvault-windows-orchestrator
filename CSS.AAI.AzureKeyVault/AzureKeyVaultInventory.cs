using Microsoft.Azure.KeyVault;
using System;
using System.Collections.Generic;
using System.Linq;
using Keyfactor.Platform.Extensions.Agents;                    // Release 5.1.0.0 - Runtime v4.0.30319
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Keyfactor.Platform.Extensions.Agents.Interfaces;

namespace CSS.AAI.AzureKeyVault
{
    public class AzureKeyVaultInventory : AzureKeyVaultJob, IAgentJobExtension
    {
        #region Interface Implementation

        public string GetJobClass()
        {
            return "Inventory";
        }

        public string GetStoreType()
        {
            return StoreTypeName;
        }

        public AnyJobCompleteInfo processJob(
                                    AnyJobConfigInfo config, 
                                    SubmitInventoryUpdate submitInventory, 
                                    SubmitEnrollmentRequest submitEnrollmentRequest, 
                                    SubmitDiscoveryResults sdr
        )
        {
            // declare local variables 
            List<AgentCertStoreInventoryItem> inventoryItems = new List<AgentCertStoreInventoryItem>();

            #region Job Initialization 
            try
            {
                Initialize(config.Store.Properties.ToString(), config.Store.StorePath);
                Logger.Trace($"Configuration for {GetJobClass()} {GetStoreType()} complete.");
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Initialization", GetJobClass, GetStoreType);
            }
            #endregion

            #region Collection 
            try
            {
                var certCollection = KV_SDK_Client.GetCertificatesAsync(JobConfiguration.VaultURL).Result;
                Logger.Debug($"Found {certCollection.Count()} Total Certificates in Azure Key Vault.");

                foreach (var certificateListing in certCollection)
                {
                    // the items returned by the list only include the thumbprint so we call this for more detail
                    var certificate = KV_SDK_Client.GetCertificateAsync(certificateListing.Id).Result;

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
                return ThrowError(ex, "Collection", GetJobClass, GetStoreType);
            }
            #endregion

            // upload to CMS
            submitInventory.Invoke(inventoryItems);

            return new AnyJobCompleteInfo()
            {
                Status = 2,
                Message = "Inventory Complete"
            };
        }

        #endregion
    }
}
