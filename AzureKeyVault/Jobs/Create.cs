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
