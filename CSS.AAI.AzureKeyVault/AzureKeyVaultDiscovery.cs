using System;
using Keyfactor.Platform.Extensions.Agents;                    // Release 5.1.0.0 - Runtime v4.0.30319
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using CSS.Common.Logging;

namespace CSS.AAI.AzureKeyVault
{
    public class AzureKeyVaultDiscovery : AzureKeyVaultJob, IAgentJobExtension
    {
        #region Interface Implementation

        public string GetJobClass()
        {
            return "Discovery";
        }

        public string GetStoreType()
        {
            return StoreTypeName;
        }

        public AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            Logger.MethodEntry();

            List<string> keyVaults = new List<string>();
            string managementToken;
            Utilities._DiscoveryResult result;

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
                return ThrowError(ex, "Initialization", GetJobClass, GetStoreType);
            }
            #endregion

            #region Discovery

            try
            {
                result = ListVaults(
                    $"https://management.azure.com/subscriptions/{JobConfiguration.SubscriptionId}/resources?%24filter=resourceType%20eq%20%27Microsoft.KeyVault%2Fvaults%27&api-version=2018-05-01",
                    managementToken
                ).Result;
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Initialization", GetJobClass, GetStoreType);
            }

            foreach (var keyVault in result.Vaults)
                keyVaults.Add(keyVault.Name);

            #endregion

            // submit results to CMS
            sdr.Invoke(keyVaults);

            return new AnyJobCompleteInfo()
            {
                Status = 2,
                Message = "Discovery Complete"
            };
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
        private static async Task<Utilities._DiscoveryResult> ListVaults(string uri, string token)
        {
            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
            HttpResponseMessage resp = await client.GetAsync(uri);

            return JsonConvert.DeserializeObject<Utilities._DiscoveryResult>(resp.Content.ReadAsStringAsync().Result);
        }

        #endregion
    }
}
