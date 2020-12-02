using System;
using AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;
using Microsoft.Azure.Management.KeyVault.Models;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    [Job(JobTypes.CREATE)]
    public class Create : AzureKeyVaultJob, IAgentJobExtension
    {
        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            Vault result;

            Initialize(config);

            try
            {
                result = AzClient.CreateVault().Result;
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Create Vault");
            }

            if (result.Id.Contains(JobParameters.VaultName)) return Success();

            return ThrowError(
                    new Exception("The creation of the Azure Key Vault failed for an unknown reason. Check your job parameters and ensure permissions are correct."),
                    "Creating Azure Key Vault");
        }
    }
}
