#!/usr/bin/env bash
# Linux/macOS equivalent of build.cmd + build.ps1
#
#  /               - This is where the project build code lives
#  /build          - This folder is created if it is missing and contains output of the build
#  /src            - This folder contains the source code or solutions you want to build
#
# Set BUILD_SUFFIX to pack with a version suffix, e.g. BUILD_SUFFIX=beta1 ./build.sh
set -euo pipefail

build_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
build_artifacts_dir="${BUILD_ARTIFACTS_DIR:-$build_dir/build}"

projects=(
    "$build_dir/src/NPoco"
    "$build_dir/src/NPoco.JsonNet"
    "$build_dir/src/NPoco.SqlServer"
    "$build_dir/src/NPoco.SqlServer.SystemData"
    "$build_dir/src/NPoco.Abstractions"
    "$build_dir/src/NPoco.FluentSql"
)

task() {
    printf '%s[%s]%s\n' "-------------------------" "$1" "-------------------------"
}

clean() {
    task Clean
    echo "Creating BuildArtifacts directory"
    rm -rf "$build_artifacts_dir"
    mkdir -p "$build_artifacts_dir"
}

build() {
    task Build
    echo "Creating BuildArtifacts"
    (cd "$build_dir" && dotnet restore)

    suffix=()
    if [[ -n "${BUILD_SUFFIX:-}" ]]; then
        suffix=("/p:VersionSuffix=$BUILD_SUFFIX")
    fi

    for project in "${projects[@]}"; do
        (cd "$project" && dotnet pack --configuration release --output "$build_artifacts_dir" "${suffix[@]}")
    done
}

clean
build
