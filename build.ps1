#This build assumes the following directory structure
#
#  \               - This is where the project build code lives
#  \build          - This folder is created if it is missing and contains output of the build
#  \src            - This folder contains the source code or solutions you want to build
#
Properties {
    $build_dir = Split-Path $psake.build_script_file    
    $build_artifacts_dir = "$build_dir\build"
    $solution_dir = "$build_dir\src\NPoco"
    $jsonnet = "$build_dir\src\NPoco.JsonNet"
    $sqlserver = "$build_dir\src\NPoco.SqlServer"
}

FormatTaskName (("-"*25) + "[{0}]" + ("-"*25))

Task Default -depends Build 

Task Build -depends Clean {
    Write-Host "Creating BuildArtifacts" -ForegroundColor Green
    Exec { dotnet restore }
    Set-Location "$solution_dir"
    if ($env:BUILD_SUFFIX -ne "") {
       $suffix = "/p:VersionSuffix=""$env:BUILD_SUFFIX"""
    }
    Exec { dotnet pack --configuration release --output $build_artifacts_dir $suffix } 
    Set-Location "$jsonnet"
    Exec { dotnet pack --configuration release --output $build_artifacts_dir $suffix } 
    Set-Location "$sqlserver"
    Exec { dotnet pack --configuration release --output $build_artifacts_dir $suffix } 
}

Task Clean {
    Write-Host "Creating BuildArtifacts directory" -ForegroundColor Green
    if (Test-Path $build_artifacts_dir) {
        rd $build_artifacts_dir -rec -force | out-null
    }
    
    mkdir $build_artifacts_dir | out-null
}