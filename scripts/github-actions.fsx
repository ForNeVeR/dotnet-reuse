let licenseHeader = """
# SPDX-FileCopyrightText: 2024-2025 Friedrich von Never <friedrich@fornever.me>
#
# SPDX-License-Identifier: MIT

# This file is auto-generated.""".Trim()

#r "nuget: Generaptor.Library, 1.8.0"

open Generaptor
open Generaptor.GitHubActions
open type Generaptor.GitHubActions.Commands

let workflows = [

    let workflow name steps =
        workflow name [
            header licenseHeader
            yield! steps
        ]

    let dotNetJob id steps =
        job id [
            setEnv "DOTNET_CLI_TELEMETRY_OPTOUT" "1"
            setEnv "DOTNET_NOLOGO" "1"
            setEnv "NUGET_PACKAGES" "${{ github.workspace }}/.github/nuget-packages"

            step(
                name = "Check out the sources",
                usesSpec = Auto "actions/checkout"
            )
            step(
                name = "Set up .NET SDK",
                usesSpec = Auto "actions/setup-dotnet"
            )
            step(
                name = "Cache NuGet packages",
                usesSpec = Auto "actions/cache",
                options = Map.ofList [
                    "key", "${{ runner.os }}.nuget.${{ hashFiles('**/*.*proj', '**/*.props') }}"
                    "path", "${{ env.NUGET_PACKAGES }}"
                ]
            )

            yield! steps
        ]

    workflow "main" [
        name "Main"
        onPushTo "main"
        onPullRequestTo "main"
        onSchedule "0 0 * * 6"
        onWorkflowDispatch

        dotNetJob "verify-workflows" [
            runsOn "ubuntu-24.04"
            step(run = "dotnet fsi ./scripts/github-actions.fsx verify")
        ]

        dotNetJob "check" [
            strategy(failFast = false, matrix = [
                "image", [
                    "macos-14"
                    "ubuntu-24.04"
                    "ubuntu-24.04-arm"
                    "windows-11-arm"
                    "windows-2025"
                ]
            ])
            runsOn "${{ matrix.image }}"

            step(
                name = "Build",
                run = "dotnet build"
            )
            step(
                name = "Test",
                run = "dotnet test",
                timeoutMin = 10
            )
        ]

        dotNetJob "check-docs" [
            runsOn "ubuntu-24.04"
            step(
                name = "Restore dotnet tools",
                run = "dotnet tool restore"
            )
            step(
                name = "Validate docfx",
                run = "dotnet docfx docs/docfx.json --warningsAsErrors"
            )
        ]

        dotNetJob "check-all-warnings" [ // separate check not bothering the local compilation
            runsOn "ubuntu-24.04"
            step(name = "Verify with full warning check", run = "dotnet build -p:AllWarningsMode=true")
        ]

        job "licenses" [
            runsOn "ubuntu-24.04"
            step(
                name = "Check out the sources",
                usesSpec = Auto "actions/checkout"
            )
            step(
                name = "REUSE license check",
                usesSpec = Auto "fsfe/reuse-action"
            )
        ]

        job "encoding" [
            runsOn "ubuntu-24.04"
            step(
                name = "Check out the sources",
                usesSpec = Auto "actions/checkout"
            )
            step(
                name = "Verify encoding",
                shell = "pwsh",
                run = "Install-Module VerifyEncoding -Repository PSGallery -RequiredVersion 2.2.1 -Force && Test-Encoding"
            )
        ]
    ]

    workflow "release" [
        name "Release"
        onPushTo "main"
        onPushTags "v*"
        onPullRequestTo "main"
        onSchedule "0 0 * * 6"
        onWorkflowDispatch
        dotNetJob "nuget" [
            jobPermission(PermissionKind.Contents, AccessKind.Write)
            runsOn "ubuntu-24.04"
            step(
                id = "version",
                name = "Get version",
                shell = "pwsh",
                run = "echo \"version=$(scripts/Get-Version.ps1 -RefName $env:GITHUB_REF)\" >> $env:GITHUB_OUTPUT"
            )
            step(
                run = "dotnet pack --configuration Release -p:Version=${{ steps.version.outputs.version }}"
            )
            step(
                name = "Read changelog",
                usesSpec = Auto "ForNeVeR/ChangelogAutomation.action",
                options = Map.ofList [
                    "output", "./release-notes.md"
                ]
            )
            step(
                name = "Upload artifacts",
                usesSpec = Auto "actions/upload-artifact",
                options = Map.ofList [
                    "path", "./release-notes.md\n./FVNever.Reuse/bin/Release/FVNever.Reuse.${{ steps.version.outputs.version }}.nupkg\n./FVNever.Reuse/bin/Release/FVNever.Reuse.${{ steps.version.outputs.version }}.snupkg"
                ]
            )
            step(
                condition = "startsWith(github.ref, 'refs/tags/v')",
                name = "Create a release",
                usesSpec = Auto "softprops/action-gh-release",
                options = Map.ofList [
                    "body_path", "./release-notes.md"
                    "files", "./FVNever.Reuse/bin/Release/FVNever.Reuse.${{ steps.version.outputs.version }}.nupkg\n./FVNever.Reuse/bin/Release/FVNever.Reuse.${{ steps.version.outputs.version }}.snupkg"
                    "name", "dotnet-reuse v${{ steps.version.outputs.version }}"
                ]
            )
            step(
                condition = "startsWith(github.ref, 'refs/tags/v')",
                name = "Push artifact to NuGet",
                run = "dotnet nuget push ./FVNever.Reuse/bin/Release/FVNever.Reuse.${{ steps.version.outputs.version }}.nupkg --source https://api.nuget.org/v3/index.json --api-key ${{ secrets.NUGET_TOKEN }}"
            )
        ]
    ]

    workflow "docs" [
        name "Docs"
        onPushTo "main"
        onWorkflowDispatch
        workflowPermission(PermissionKind.Actions, AccessKind.Read)
        workflowPermission(PermissionKind.Pages, AccessKind.Write)
        workflowPermission(PermissionKind.IdToken, AccessKind.Write)
        workflowConcurrency(
            group = "pages",
            cancelInProgress = false
        )
        dotNetJob "publish-docs" [
            environment(name = "github-pages", url = "${{ steps.deployment.outputs.page_url }}")
            runsOn "ubuntu-24.04"

            step(
                name = "Set up .NET tools",
                run = "dotnet tool restore"
            )
            step(
                name = "Build the documentation",
                run = "dotnet docfx docs/docfx.json"
            )
            step(
                name = "Upload artifact",
                usesSpec = Auto "actions/upload-pages-artifact",
                options = Map.ofList [
                    "path", "docs/_site"
                ]
            )
            step(
                name = "Deploy to GitHub Pages",
                id = "deployment",
                usesSpec = Auto "actions/deploy-pages"
            )
        ]
    ]
]
exit <| EntryPoint.Process fsi.CommandLineArgs workflows
