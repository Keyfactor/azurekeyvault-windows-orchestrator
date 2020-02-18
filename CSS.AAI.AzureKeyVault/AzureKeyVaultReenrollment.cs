using System;
using Keyfactor.Platform.Extensions.Agents;                    // Release 5.1.0.0 - Runtime v4.0.30319
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Microsoft.Azure.KeyVault;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using System.Linq;

namespace CSS.AAI.AzureKeyVault
{
    public class AzureKeyVaultReenrollment : AzureKeyVaultJob, IAgentJobExtension
    {
        #region Interface Implementation

        public string GetJobClass()
        {
            return "Enrollment";
        }

        public string GetStoreType()
        {
            return StoreTypeName;
        }

        public AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = "Invalid Management Operation"
            };

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

            #region Reenroll

            try
            {
                var csrContent = KV_SDK_Client.CreateCertificateAsync(JobConfiguration.VaultURL, config.Job.Alias).Result.Csr;
                submitEnrollmentRequest.Invoke(Convert.ToBase64String(csrContent));
            }

            catch(Exception ex)
            {
                return ThrowError(ex, "Reenrollment API Call", GetJobClass, GetStoreType);
            }

            #endregion

            return complete;
        }

        #endregion

        #region Class Methods



        #endregion
    }
}