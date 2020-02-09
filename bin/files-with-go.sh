#!/usr/bin/env bash
cat ~/Dropbox/Statistics/Arbetsformedlingen/langs.txt | grep go \
    | sed 's/ .*//' \
    | xargs -I{} cat ~/Dropbox/Statistics/Arbetsformedlingen/data/{}.json | jq .platsannons.annons.annonstext \
    | grep -o -i '.......[ \t\n,.!]go[ \t\n,.!]........'

# cat ~/Dropbox/Statistics/Arbetsformedlingen/data/8475801.json  | jq .platsannons.annons.annonstext -r | grep -o -i '.......go........'