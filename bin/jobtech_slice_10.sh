#!/usr/bin/env bash
cat src/Stacka.Jobtech/jobtech-2020-02-27.json | jq '[.[range(0,10)]]' > src/Stacka.Jobtech/sample_10.json