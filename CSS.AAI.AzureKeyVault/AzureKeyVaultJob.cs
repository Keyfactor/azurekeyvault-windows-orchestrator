using Keyfactor.Platform.Extensions.Agents;                        // Release 5.1.0.0 - Runtime v4.0.30319
using CSS.Common.Logging;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSS.AAI.AzureKeyVault
{
    public class AzureKeyVaultJob : LoggingClientBase
    {
        #region Class Properties

        protected KeyVaultClient KV_SDK_Client { get; set; }

        protected Utilities._AKV_JobParameters JobConfiguration { get; set; }

        protected string StoreTypeName { get; private set; } = Utilities.StoreTypeName;

        #endregion

        #region Shared Methods

        /// <summary>
        /// Initializes the Azure SDK object and saves the store path
        /// </summary>
        /// <param name="configuration_JSON_">AnyJobConfigInfo.Store.Parameters.ToString()</param>
        /// <param name="storePath_">VaultURL</param>
        protected virtual void Initialize(string configuration_JSON_, string storePath_)
        {
            KV_SDK_Client = new KeyVaultClient(
                                    new KeyVaultClient.AuthenticationCallback(GetAccessTokenAsync),
                                    new HttpClient()
                            );

            JobConfiguration = JsonConvert.DeserializeObject<Utilities._AKV_JobParameters>(configuration_JSON_);
            JobConfiguration.VaultURL = storePath_;
        }

        /// <summary>
        /// Authentication call back method for the Azure Key Vault SDK
        /// </summary>
        /// <returns>Bearer access token</returns>
        private async Task<string> GetAccessTokenAsync(string authority, string resource, string scope)
        {
            var appCredentials = new ClientCredential(JobConfiguration.ApplicationId, JobConfiguration.ClientSecret);
            var context = new AuthenticationContext(authority, TokenCache.DefaultShared);

            var result = await context.AcquireTokenAsync(resource, appCredentials);

            return result.AccessToken;
        }

        /// <summary>
        /// Call when application encounters an error to log the error and return the error object
        /// </summary>
        /// <param name="exceptionMessage_">Message to log</param>
        /// <returns>Error AnyJobCompleteInfo object</returns>
        protected AnyJobCompleteInfo ThrowError(Exception exception, string jobSection_, Func<string> GetJobClass, Func<string> GetStoreType)
        {
            string message = FlattenException(exception);
            Logger.Error($"Error performing {jobSection_} in {GetJobClass()} {GetStoreType()} - {message}");
            return new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = message
            };
        }

        private string FlattenException(Exception ex)
        {
            string returnMessage = ex.Message;
            if (ex.InnerException != null)
                returnMessage += (" - " + FlattenException(ex.InnerException));

            return returnMessage;
        }

        #endregion
    }
}
