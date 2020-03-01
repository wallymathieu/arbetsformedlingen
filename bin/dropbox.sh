#!/usr/bin/env bash
pushd $(dirname "${0}") > /dev/null
BASEDIR=$(pwd -L)
cd ../src/Stacka.Jobtech
dotnet run --dir ~/Dropbox/Statistics/Arbetsformedlingen "$@"
