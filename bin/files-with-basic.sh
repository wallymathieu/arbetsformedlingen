#!/usr/bin/env bash
cat ~/Dropbox/Statistics/Arbetsformedlingen/langs.txt | grep basic \
    | sed 's/ .*//' \
    | xargs -I{} cat ~/Dropbox/Statistics/Arbetsformedlingen/data/{}.json | jq .platsannons.annons.annonstext \
    | grep -o -i '.......[ \t\n,.!]basic[ \t\n,.!]........'

