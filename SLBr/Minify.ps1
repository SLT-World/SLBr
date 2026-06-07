param([string]$PublishDir)

$ResourcesPath = Join-Path $PublishDir "Resources"
if (-not (Test-Path $ResourcesPath)) { return }

$UserFolder = [System.Environment]::GetFolderPath('UserProfile')
$NUglifyBasePath = Join-Path $UserFolder ".nuget\packages\nuglify"

if (-not (Test-Path $NUglifyBasePath)) {
    Write-Error "NUglify package is not installed."
    exit 1
}

$LatestVersionPath = Get-ChildItem -Path $NUglifyBasePath -Directory | Sort-Object { [version]$_.Name } -Descending | Select-Object -First 1
$NUglifyDll = Join-Path $LatestVersionPath.FullName "lib\netstandard2.0\NUglify.dll"
if (-not (Test-Path $NUglifyDll)) {
    Write-Error "Could not resolve NUglify.dll inside $($LatestVersionPath.FullName)"
    exit 1
}

[System.Reflection.Assembly]::LoadFrom($NUglifyDll) | Out-Null

Write-Host "Loaded NUglify version: $($LatestVersionPath.Name)"
Write-Host "Executing minifier on: $ResourcesPath"

$Files = Get-ChildItem -Path $ResourcesPath -Recurse -Include *.html, *.css, *.js

foreach ($File in $Files) {
    $Extension = $File.Extension.ToLower()
    $Result = $null
    try {
        if ($Extension -eq '.js') {
            $Content = [System.IO.File]::ReadAllText($File.FullName)
            $Result = [NUglify.Uglify]::Js($Content) 
        }
        elseif ($Extension -eq '.css') {
            $Content = [System.IO.File]::ReadAllText($File.FullName)
            $Result = [NUglify.Uglify]::Css($Content) 
        }
        elseif ($Extension -eq '.html') {
            $Content = [System.IO.File]::ReadAllText($File.FullName)
            $Result = [NUglify.Uglify]::Html($Content) 
        }

        if ($Result -ne $null) {
            if ($Result.HasErrors) {
                Write-Warning "Skipped: $($File.Name)"
            }
            else {
                [System.IO.File]::WriteAllText($File.FullName, $Result.Code)
                Write-Host "Minified: $($File.Name)"
            }
        }
    }
    catch {
        Write-Warning "Failed $($File.Name): $_"
    }
}