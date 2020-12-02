using System;
using AzureKeyVault;
using Keyfactor.Platform.Extensions.Agents;
using Keyfactor.Platform.Extensions.Agents.Delegates;
using Keyfactor.Platform.Extensions.Agents.Interfaces;

namespace Keyfactor.AnyAgent.AzureKeyVault
{
    [Job(JobTypes.REENROLLMENT)]
    public class Reenrollment : AzureKeyVaultJob, IAgentJobExtension
    {
        public override AnyJobCompleteInfo processJob(AnyJobConfigInfo config, SubmitInventoryUpdate submitInventory, SubmitEnrollmentRequest submitEnrollmentRequest, SubmitDiscoveryResults sdr)
        {
            Initialize(config);

            AnyJobCompleteInfo complete = new AnyJobCompleteInfo()
            {
                Status = 4,
                Message = "Invalid Management Operation"
            };

            try
            {
                var csrContent = AzClient.CreateCertificateAsync(config.Job.Alias).Result.Csr;
                submitEnrollmentRequest.Invoke(Convert.ToBase64String(csrContent));
            }

            catch (Exception ex)
            {
                return ThrowError(ex, "Reenrollment API Call");
            }

            complete.Status = 2;
            complete.Message = "Reenrollment Complete";
            return complete;
        }
    }
}
