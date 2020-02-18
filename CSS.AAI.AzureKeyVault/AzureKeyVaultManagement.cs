using Keyfactor.Platform.Extensions.Agents;                    // Release 5.1.0.0 - Runtime v4.0.30319
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Microsoft.Azure.KeyVault;
using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace CSS.AAI.AzureKeyVault
{
    class AzureKeyVaultManagement : AzureKeyVaultJob, IAgentJobExtension
    {
        #region Interface Implementation

        public string GetJobClass()
        {
            return "Management";
        }

        public string GetStoreType()
        {
            return StoreTypeName;
        }

        public AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
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

            AnyJobCompleteInfo complete = new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = "Invalid Management Operation"
            };

            if (config.Job.OperationType.ToString() == "Add")
            {
                complete = PerformAddition(config);
            }
            else if (config.Job.OperationType.ToString() == "Remove")
            {
                complete = PerformRemoval(config);
            }

            return complete;
        }

        #endregion

        #region Class Methods

        private AnyJobCompleteInfo PerformRemoval(AnyJobConfigInfo config)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo();

            if (String.IsNullOrWhiteSpace(config.Job.Alias))
            {
                complete.Status = 4;
                complete.Message = "You must supply an alias for the certificate.";

                return complete;
            }

            try
            {
                var result = KV_SDK_Client.DeleteCertificateAsync(JobConfiguration.VaultURL, config.Job.Alias).Result;

                if (result.CertificateIdentifier.Name == config.Job.Alias)
                {
                    complete.Status = 2;
                    complete.Message = "";

                }
                else
                {
                    complete.Status = 4;
                    complete.Message = complete.Message = $"Unable to remove {config.Job.Alias} from {GetStoreType()}. Check your network connection, ensure the password is correct, and that your API connection information is correct.";
                }
            }
            
            catch (Exception ex)
            {
                complete.Status = 4;
                complete.Message = $"An error occured while removing {config.Job.Alias} from {GetStoreType()}: " + ex.Message;
            }

            return complete;
        }

        private AnyJobCompleteInfo PerformAddition(AnyJobConfigInfo config)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo();
            X509Certificate2Collection uploadCollection = null;

            if (!String.IsNullOrWhiteSpace(config.Job.PfxPassword)) // This is a PFX Entry
            {
                if (String.IsNullOrWhiteSpace(config.Job.Alias))
                {
                    complete.Status = 4;
                    complete.Message = "You must supply an alias for the certificate.";

                    return complete;
                }

                #region Load PFX
                try
                {
                    byte[] pfxBytes = Convert.FromBase64String(config.Job.EntryContents);

                    uploadCollection = new X509Certificate2Collection();
                    uploadCollection.Add(
                        new X509Certificate2(pfxBytes, config.Job.PfxPassword, X509KeyStorageFlags.Exportable)
                    );
                }

                catch (Exception ex)
                {
                    complete.Status = 4;
                    complete.Message = $"An error occured trying to create decode the provided certificate with alias {config.Job.Alias}: " + ex.Message;
                }
                #endregion

                #region Upload Cert
                try
                {
                    // uploadCollection is either not null or an exception was thrown.

                    var success = KV_SDK_Client.ImportCertificateAsync(JobConfiguration.VaultURL, config.Job.Alias, uploadCollection, null).Result;

                    // Ensure the return object has a AKV version tag, and Thumbprint
                    if (!string.IsNullOrEmpty(success.CertificateIdentifier.Version) && 
                        !string.IsNullOrEmpty(string.Concat(success.X509Thumbprint.Select(i => i.ToString("X2"))))
                    )
                    {
                        complete.Status = 2;
                        complete.Message = "";
                    }
                    else
                    {
                        complete.Status = 4;
                        complete.Message = $"Unable to add {config.Job.Alias} to {GetStoreType()}. Check your network connection, ensure the password is correct, and that your API connection information is correct.";
                    }
                }

                catch (Exception ex)
                {
                    complete.Status = 4;
                    complete.Message = $"An error occured while adding {config.Job.Alias} to {GetStoreType()}: " + ex.Message;

                    if (ex.InnerException != null)
                        complete.Message += " - " + ex.InnerException.Message;
                }
                #endregion
            }

            else  // Non-PFX
            {
                complete.Status = 4;
                complete.Message = "Certificate to add must be in a .PFX file format.";
            }

            return complete;
        }

        #endregion
    }
}
