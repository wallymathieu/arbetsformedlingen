#!/usr/bin/env bash
curl -X GET "https://jobstream.api.jobtechdev.se/stream?date=1990-01-01T01%3A29%3A44" -H "accept: application/json" -H "api-key: ${JOBTECHDEV_APIKEY}"