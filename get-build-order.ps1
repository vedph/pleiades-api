# Get-BuildOrder.ps1
# Run this from your solution root directory once to generate
# the correct project order for buildnpub.ps1

$repoRoot = $PSScriptRoot
$csprojFiles = Get-ChildItem -Path $repoRoot -Filter *.csproj -Recurse |
    Where-Object { $_.BaseName -notlike "*.Test" }

$projects = @{}
$projectPaths = @()

# 1. Parse all projects and map their internal ProjectReferences
foreach ($file in $csprojFiles) {
    # Get path relative to the script root to match your array format
    $relativePath = Resolve-Path $file.FullName -Relative
    $relativePath = $relativePath -replace '^\.[\\/]', ''

    # Read the csproj XML cleanly
    [xml]$xml = Get-Content $file.FullName

    # Extract referenced project file names
    $references = @()
    if ($xml.Project.ItemGroup) {
        $refNodes = $xml.Project.ItemGroup.ProjectReference
        if ($refNodes) {
            foreach ($ref in $refNodes) {
                if ($ref.Include) {
                    # Get just the file name (e.g., "Cadmus.Core.csproj")
                    $refName = Split-Path $ref.Include -Leaf
                    $references += $refName
                }
            }
        }
    }

    $fileName = $file.Name # e.g., "Cadmus.Core.csproj"
    $projects[$fileName] = @{
        RelativePath = $relativePath
        Dependencies = $references
    }
    $projectPaths += $fileName
}

# 2. Topological Sort (DFS)
$visited = @{}
# Use a .NET List so Add() mutates the same instance from inside the function;
# a PowerShell array with += inside a function would rebind a local copy instead.
$sortedList = [System.Collections.Generic.List[string]]::new()

function Visit-Project($projectName) {
    if ($visited[$projectName] -eq "Visiting") {
        Write-Error "Circular dependency detected involving $projectName!"
        exit 1
    }

    if (-not $visited[$projectName]) {
        $visited[$projectName] = "Visiting"

        # Visit all dependencies first
        if ($projects[$projectName]) {
            foreach ($dep in $projects[$projectName].Dependencies) {
                # Only visit if the project belongs to our solution map (and isn't an excluded test project)
                if ($projects[$dep]) {
                    Visit-Project $dep
                }
            }
        }

        $visited[$projectName] = "Visited"
        $sortedList.Add($projects[$projectName].RelativePath)
    }
}

foreach ($proj in $projectPaths) {
    Visit-Project $proj
}

# 3. Output the formatting formatted perfectly for your buildnpub.ps1 array
Write-Host "`n=== Copy the array below into your buildnpub.ps1 ===`n" -ForegroundColor Cyan
Write-Output "@("
foreach ($path in $sortedList) {
    Write-Output "    `"$path`","
}
Write-Output ")"
