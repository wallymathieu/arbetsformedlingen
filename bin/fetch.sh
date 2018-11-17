#!/usr/bin/env bash
pushd $(dirname "${0}") > /dev/null
BASEDIR=$(pwd -L)
cd ../src/Core/
dotnet run --dir ~/Dropbox/Statistics/Arbetsformedlingen --command fetch-files
