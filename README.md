# RaceDay - Event Management System (POE Part 1)

## Overview
**RaceDay** is an event management platform built for the South African endurance sports community (running, cycling, and walking). The solution enables **Organisers** to host events and publish official race results, while allowing **Participants** to browse events, enrol, and track performance history.

---

## Roles & Permissions

* **Organiser:**
  * Create, manage, and update race events.
  * Define event categories, distances, and entry fees.
  * Capture participant completion times and race rankings.

* **Participant:**
  * Search and view upcoming events and categories.
  * Enrol in race events.
  * View personal enrolment history and finish times.

---

## Database Setup (SSMS)

1. Open **SQL Server Management Studio (SSMS)** and connect to your local server instance.
2. Open `/docs/schema.sql`.
3. Run the script to create the `RaceDayDb` database and load initial seed data for roles, users, events, categories, enrolments, and results.

---

## Repository Documentation (`/docs`)

* **ERD Diagram:** `docs/erd.png`
* **API Endpoint Plan:** `docs/endpoint-plan.md`
* **Database Schema Script:** `docs/schema.sql`

---

## CI/CD Pipeline Status
![CI Validation Workflow](https://github.com/ST10449855/RaceDay/actions/workflows/validate-docs.yml/badge.svg)

---

## Video Walkthrough Presentation
* **YouTube Video Link:** [Insert Unlisted YouTube Link Here]

---

## References

* GeeksforGeeks. (2026). *C# Programming Language Resources & Tutorials*. Available at: https://www.geeksforgeeks.org/search/?gq=C (Accessed: 22 September 2026).