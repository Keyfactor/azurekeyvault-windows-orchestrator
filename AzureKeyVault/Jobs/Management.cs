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
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Enums;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Microsoft.Azure.Management.KeyVault.Models;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    [Job(JobTypes.MANAGEMENT)]
    public class Management : AzureKeyVaultJob, IAgentJobExtension
    {
        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            Initialize(config);

            AnyJobCompleteInfo complete = new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = "Invalid Management Operation"
            };

            switch (config.Job.OperationType)
            {
                case AnyJobOperationType.Create:
                    complete = PerformCreateVault();
                    break;
                case AnyJobOperationType.Add:
                    complete = PerformAddition(config.Job.Alias, config.Job.PfxPassword, config.Job.EntryContents);
                    break;
                case AnyJobOperationType.Remove:
                    complete = PerformRemoval(config.Job.Alias);
                    break;
            }

            return complete;
        }

        #region Create

        protected virtual AnyJobCompleteInfo PerformCreateVault()
        {
            Vault result;

            try
            {
                result = AzClient.CreateVault().Result;
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Creating Azure Key Vault");
            }

            if (result.Id.Contains(JobParameters.VaultName))
                return new AnyJobCompleteInfo()
                {
                    Status = 2,
                    Message = "Create Vault Complete"
                };
            else
                return ThrowError(
                    new Exception("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct."),
                    "Creating Azure Key Vault");
        }

        #endregion

        #region Add
        protected virtual AnyJobCompleteInfo PerformAddition(string alias, string pfxPassword, string entryContents)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo();
            X509Certificate2Collection uploadCollection = null;

            if (!string.IsNullOrWhiteSpace(pfxPassword)) // This is a PFX Entry
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    complete.Status = 4;
                    complete.Message = "You must supply an alias for the certificate.";

                    return complete;
                }

                try
                {
                    uploadCollection = GenerateCertificate(pfxPassword, entryContents);
                }
                catch (Exception ex)
                {
                    complete.Status = 4;
                    complete.Message = $"An error occured trying to create decode the provided certificate with alias {alias}: " + ex.Message;
                }

                try
                {
                    // uploadCollection is either not null or an exception was thrown.
                    var success = AzClient.ImportCertificateAsync(alias, uploadCollection).Result;

                    // Ensure the return object has a AKV version tag, and Thumbprint
                    if (!string.IsNullOrEmpty(success.CertificateIdentifier.Version) &&
                        !string.IsNullOrEmpty(string.Concat(success.X509Thumbprint.Select(i => i.ToString("X2"))))
                    )
                    {
                        complete.Status = 2;
                        complete.Message = $"Successfully Added {alias}";
                    }
                    else
                    {
                        complete.Status = 4;
                        complete.Message = $"Unable to add {alias} to {GetStoreType()}. Check your network connection, ensure the password is correct, and that your API connection information is correct.";
                    }
                }

                catch (Exception ex)
                {
                    complete.Status = 4;
                    complete.Message = $"An error occured while adding {alias} to {GetStoreType()}: " + ex.Message;

                    if (ex.InnerException != null)
                        complete.Message += " - " + ex.InnerException.Message;
                }
            }

            else  // Non-PFX
            {
                complete.Status = 4;
                complete.Message = "Certificate to add must be in a .PFX file format.";
            }

            return complete;
        }

        #endregion

        #region Remove

        protected virtual AnyJobCompleteInfo PerformRemoval(string alias)
        {
            AnyJobCompleteInfo complete = new AnyJobCompleteInfo();

            if (string.IsNullOrWhiteSpace(alias))
            {
                complete.Status = 4;
                complete.Message = "You must supply an alias for the certificate.";
                return complete;
            }

            try
            {
                var result = AzClient.DeleteCertificateAsync(alias).Result;

                if (result.CertificateIdentifier.Name == alias)
                {
                    complete.Status = 2;
                    complete.Message = $"Successfully removed {alias}";
                }
                else
                {
                    complete.Status = 4;
                    complete.Message = complete.Message = $"Unable to remove {alias} from {GetStoreType()}. Check your network connection, ensure the password is correct, and that your API connection information is correct.";
                }
            }

            catch (Exception ex)
            {
                complete.Status = 4;
                complete.Message = $"An error occured while removing {alias} from {GetStoreType()}: " + ex.Message;
            }

            return complete;
        }

        #endregion

        protected virtual X509Certificate2Collection GenerateCertificate(string pfxPassword, string content)
        {
            X509Certificate2Collection uploadCollection = new X509Certificate2Collection();

            byte[] pfxBytes = Convert.FromBase64String(content);

            uploadCollection = new X509Certificate2Collection();
            uploadCollection.Add(
                new X509Certificate2(pfxBytes, pfxPassword, X509KeyStorageFlags.Exportable)
            );
            return uploadCollection;
        }
    }
}

