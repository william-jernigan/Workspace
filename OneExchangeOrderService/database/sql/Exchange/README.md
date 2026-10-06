# Introduction

This is a [Microsoft SQL Database Project](https://learn.microsoft.com/en-us/sql/ssdt/project-oriented-offline-database-development?view=sql-server-ver16) that used by the service/app in the same repo.

# Getting Started

This database project was added PRIOR to adding database project support to [OneSystem Service Template](https://hiltongrandvacations.visualstudio.com/HGVC/_wiki/wikis/HGVC.wiki/918/OneSystem-Template).

Refer to [OneVision Wiki - Database Projects](https://hiltongrandvacations.visualstudio.com/OneVision/_wiki/wikis/OneVision.wiki/1083/Database-Projects) for more information, see below for how to build, deploy and use in local development.

# Build, Deploy Locally and Use for Local Development

This project uses the [microsoft.sqlpackage](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-download?view=sql-server-ver16) dotnet tool build and deploy the database project.

#### Pre-requisites
- (Required) .NET Core SDK version 6.0 or later to be installed on your device.
- (Required) the sqlpackage dotnet tool, 
    - install the sqlpackage tool globally by running the command `dotnet tool install -g microsoft.sqlpackage`
- (Optional) For local development, a sql server instance, one of:
    1. [Docker image](https://hub.docker.com/_/microsoft-mssql-server) - `docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=yourStrong(!)Password" -e "MSSQL_PID=Evaluation" -p 1433:1433 --name sqlpreview --hostname sqlpreview -d mcr.microsoft.com/mssql/server:2022-preview-ubuntu-22.04`
    2. [SQL Server Developer Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
1. **Build** the project by running the command: `dotnet build database/sql/one-exchange-order-service/one-exchange-order-service.sqlproj`
2. **Deploy** to local dev environment by running the command ([MS docs](https://learn.microsoft.com/en-us/sql/tools/sqlpackage/sqlpackage-publish?view=sql-server-ver16)): `SqlPackage /Action:Publish /TargetTrustServerCertificate:true /SourceFile:"database/sql/party/bin/Debug/party.dacpac" /tdn:party /tsn:localhost /tu:{your-username-for-local-sql} /tp:"{your-password-for-local-sql}"`
    - ❗️ Your username and password will be unique to your local sql instance, not ANY of your HGV credentials ❗️
3. **Use** your local database in service configuration with a connection string like: `"Persist Security Info=False;User ID={your-username-for-local-sql};Password={your-password-for-local-sql}*;Server=localhost;Encrypt=True;TrustServerCertificate=true;"`

# CI/CD

The `azure-pipelines.yml` file referes to a database project pipeline template that implements both quality gates at PR-time and build/deploy when changes are mergeed to the main branch.

Refer to [Azure Repos: OneSystem - Pipelines/Database](https://hiltongrandvacations.visualstudio.com/OneVision/_git/OneSystem?path=/Pipelines/Database) for more information.