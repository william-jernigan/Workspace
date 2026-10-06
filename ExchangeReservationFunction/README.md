# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started
This service was implemented using [OneSystem Service Template](https://hiltongrandvacations.visualstudio.com/HGVC/_wiki/wikis/HGVC.wiki/918/OneSystem-Template).

# Build and Test

Use the `dotnet` CLI tool to:
1. **Restore All Projects** - `dotnet restore`
2. **Build All Projects** - `dotnet build`
3. **Run and Watch the WebApi** - `dotnet watch run --project API.Solution.Template.WebApi/API.Solution.Template.WebApi.csproj`

# CI/CD

The `merge-validation.yml` pipeline will run to execute quality gate checks for new Pull Requests.
The `azure-pipelines.yml` is the main build & release pipeline, it runs when commit is pushed to `main` - typically, a merge commit once a PR is approved and merged.

> Refer to [OneSystem/Pipelines/Container App/README.md](https://hiltongrandvacations.visualstudio.com/OneVision/_git/OneSystem?path=/Pipelines/Container%20App) and [OneSystem Template - Deployment](https://hiltongrandvacations.visualstudio.com/HGVC/_wiki/wikis/HGVC.wiki/937/Deployment) for more information about ci/cd pipelines for this project type.

# Contribute
Refer to [Source Control for OneSystem](https://hiltongrandvacations.visualstudio.com/HGVC/_wiki/wikis/HGVC.wiki/979/Source-Control-for-ONE-SYSTEM)