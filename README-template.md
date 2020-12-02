###Keyfactor integrations (draft)

We provide a number of pre-compiled extensions that allow you to integrate the platform with your existing storage and client solutions.  
We've also provided the source code for these extensions as well as an SDK interface for writing your own.

Platform Integrations:
- [Ansible]()
- [Azure Keyvault]()
- [DataPower]()
- [SAP]()
- [GoDaddy]()
- [OpenXPKI]()
- [WiseKey]()
- [Terraform]()
- [SecretsAgent]()
- [OpenSSL/WolfSSL]()

Community integrations:
Have you written your own extension for Keyfactor?  Consider [sharing it with the community](pull-request-guidelines)!   

We are always expanding our collection of integrations. 
If you need to integrate with a different platform [let us know!]()

###Installing an extension
The general process for installing an extension to the platform is as follows:
1. Create a [certificate store type](store-type-doc) for the certificate store
    - The values will correspond to the required values for the integration
1. Run the [agent configuration](agent-config-doc) on the client machine to register the integration
1. [Create the certificate store](cert-store-create-doc) in the platform.
1. Confirm that the integration was successful.

###Debugging an extension
####Visual Studio
####Visual Studio code
####Eclipse


###Creating a custom extension
Need to integrate with a different/proprietary platform, or need to customize an integration? We provide an array of tools and documentation to help you get started.



####.NET
- [SDK]()
####Java

