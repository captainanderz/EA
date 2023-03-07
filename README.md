# Introduction 
Project Horizon is the new implementation for Endpoint Admin, developed by Fortech, built on .NET 5 and Angular 11.

Endpoint Admin (EA) is a web-based application that helps system administrators deploy and patch line-of-business (LOB) applications in the software management suite Microsoft Intune.

# Installation process

## Docker support
The ProjectHorizon.WebAPI project was created with Docker support, so before getting the project you have to install Docker Desktop - https://www.docker.com/products/docker-desktop

After installing Docker and doing the required restart, you might get a message that virtualization is not enabled. You have to go in BIOS and enable it, it should be in the CPU category. After a restart Docker should start successfully. You can find more troubleshooting help for this at https://docs.docker.com/docker-for-windows/troubleshoot/#virtualization ; if stuck, contact Dan Dumitru.

## Git repository
https://dev.azure.com/endpointadmin/_git/Project%20Horizon

The main branch is named `main` (not master).

Make sure you clone the repo using your endpointadmin credentials (xx@endpointadmin.com)

In Visual Studio you can use the Git Changes view (View -> Git Changes) to see your changes, push, pull, etc.

# Running the project
In Visual Studio, set ProjectHorizon.WebAPI as the Startup Project, and execute Start Debugging (F5). A browser window should open, pointing at the Swagger API documentation.
