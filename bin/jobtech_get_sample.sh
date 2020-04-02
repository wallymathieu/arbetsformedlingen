#!/usr/bin/env bash
DATE=`date "+%Y-%m-%d"`
curl -X GET "https://jobstream.api.jobtechdev.se/stream?date=${DATE}T01%3A29%3A44" -H "accept: application/json" -H "api-key: ${JOBTECHDEV_APIKEY}"