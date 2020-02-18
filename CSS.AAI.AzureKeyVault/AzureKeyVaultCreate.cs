using System;
using Keyfactor.Platform.Extensions.Agents;                    // Release 5.1.0.0 - Runtime v4.0.30319
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Microsoft.Azure.Management.KeyVault.Models;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;

namespace CSS.AAI.AzureKeyVault
{
    public class AzureKeyVaultCreate : AzureKeyVaultJob, IAgentJobExtension
    {
        #region Interface Implementation

        public string GetJobClass()
        {
            return "Create";
        }

        public string GetStoreType()
        {
            return StoreTypeName;
        }

        public AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            string managementToken;
            Vault result;

            #region Initialization 
            try
            {
                Initialize(config.Store.Properties.ToString(), config.Store.StorePath);
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Initialization", GetJobClass, GetStoreType);
            }
            #endregion

            #region Token Request
            try
            {
                // get managerial OAuth token
                managementToken = ManagementAuthenticationHandler.AcquireTokenBySPN(JobConfiguration.TenantId, JobConfiguration.ApplicationId, JobConfiguration.ClientSecret).Result;
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Token Request", GetJobClass, GetStoreType);
            }
            #endregion

            #region Create

            try
            {
                result = CreateVault(
                    $"https://management.azure.com/subscriptions/{JobConfiguration.SubscriptionId}/resourceGroups/{JobConfiguration.ResourceGroupName}/providers/Microsoft.KeyVault/vaults/{JobConfiguration.VaultName}?api-version=2018-02-14-preview",
                    managementToken
                ).Result;
            }

            catch(Exception ex)
            {
                return ThrowError(ex, "Create API Call", GetJobClass, GetStoreType);
            }

            #endregion

            if (result.Id.Contains(JobConfiguration.VaultName))
                return new AnyJobCompleteInfo()
                {
                    Status = 2,
                    Message = "Create Complete"
                };
            else
                return ThrowError(
                    new Exception("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct."), 
                    "Creating Azure Key Vault", 
                    GetJobClass,
                    GetStoreType
                );
        }

        #endregion

        #region Class Methods

        /// <summary>
        /// Initialize the properties of the object
        /// </summary>
        /// <param name="configuration_JSON">config.Store.Properties.ToString()</param>
        protected override void Initialize(string configuration_JSON_, string storePath_)
        {
            JobConfiguration = JsonConvert.DeserializeObject<Utilities._AKV_JobParameters>(configuration_JSON_);
            JobConfiguration.VaultURL = storePath_;
        }

        /// <summary>
        /// Collects all Azure Key Vaults on the current subscription and returns them in a _DiscoverResult object
        /// </summary>
        /// <param name="uri">Path to Azure Management API</param>
        /// <param name="token">OAuth token</param>
        /// <returns>Populated _DiscoveryResult</returns>
        private async Task<Vault> CreateVault(string uri, string token)
        {
            HttpClient client = new HttpClient();

            Utilities.CreateVaultRequest req = new Utilities.CreateVaultRequest()
            {
                Location = "eastus",
                Properties = new VaultProperties()
                {
                    Sku = new Sku()
                    {
                        Name = SkuName.Standard
                    },

                    TenantId = new Guid(JobConfiguration.TenantId),
                    AccessPolicies = new List<AccessPolicyEntry>()
                    {
                        new AccessPolicyEntry()
                        {
                            ApplicationId = new Guid(JobConfiguration.ApplicationId),
                            ObjectId =JobConfiguration.APIObjectId,
                            Permissions = new Permissions()
                            {
                                Certificates = new List<string>
                                {
                                    "get",
                                    "list",
                                    "delete",
                                    "create",
                                    "import",
                                    "update",
                                    "managecontacts",
                                    "getissuers",
                                    "listissuers",
                                    "setissuers",
                                    "deleteissuers",
                                    "manageissuers",
                                    "recover",
                                    "purge"
                                },
                                Keys = new List<string>(),
                                Secrets = new List<string>(),
                                Storage = new List<string>()
                            },
                            TenantId = new Guid(JobConfiguration.TenantId)
                        }
                    }
                }
            };

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpContent content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

            HttpResponseMessage resp = await client.PutAsync(uri, content);
            return JsonConvert.DeserializeObject<Vault>(resp.Content.ReadAsStringAsync().Result);
        }

        #endregion
    }
}
