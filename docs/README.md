# RaceDay-POE - SA Road Running Events

Platform for SA road running events.

**Roles:**
- Organiser: Create events, manage registrations, capture results
- Participant: Browse events, register, view results

## Video Walkthrough Part 1
https://youtu.be/Z06iSLwJxrs?si=yxxHgkrvshpqGJ_S

## Build Status
![Green Build](docs/green-build.png)

## Planning Docs
- ERD: docs/erd.png
- API Plan: docs/api_endpoint_plan.md
- SQL: docs/RaceDay.sql

##ERD: 6 tables Users, Events, Categories, Routes, EventEnrolments and Results

## SQL Features: Check Role Organiser Participants

API Count: 27 endpoints 

Organiser can create event POST/ events

Participant can register POST/ enrolments

Auth:   JWT Bearer 

BibNumber unique per event

Build: validate.yml passing 
