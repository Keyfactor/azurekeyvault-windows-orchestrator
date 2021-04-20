// Copyright 2021 Keyfactor
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License.
// You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions
// and limitations under the License.

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
