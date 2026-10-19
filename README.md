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
* **YouTube Video Link:** [https://youtu.be/rPWAnsolIbM?si=jA6RbvMuxrdhJAod](https://youtu.be/rPWAnsolIbM?si=jA6RbvMuxrdhJAod)

---
# RaceDay System - Part 2: RESTful API Development

## Student Information
* **Name**: Aphiwe Njabulo Ndlovu
* **Student ID**: ST10449855
* **Module**: Programming 2B (PROG6212)

---

## System Overview & Roles
The **RaceDay** API is built using ASP.NET Core Web API (.NET 8) and Entity Framework Core with Code First migrations. It implements role-based session authentication for two distinct user roles:
1. **Organiser**: Can create, update, and delete events, define categories, view enrolments, and capture race results.
2. **Participant**: Can view upcoming events, register/log in, enrol in specific event categories, and view personal results.

---

## Setup and Run Instructions
1. Clone the repository and open the solution in Visual Studio.
2. Ensure your `appsettings.json` file points to your local SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RaceDayDb;Trusted_Connection=True;MultipleActiveResultSets=true"
   }
## References

* GeeksforGeeks. (2026). *C# Programming Language Resources & Tutorials*. Available at: https://www.geeksforgeeks.org/search/?gq=C (Accessed: 22 September 2026).

<!-- update log entry 1 -->

<!-- update log entry 2 -->

<!-- update log entry 3 -->

<!-- update log entry 4 -->

<!-- update log entry 5 -->

<!-- update log entry 6 -->

<!-- update log entry 7 -->

<!-- update log entry 8 -->

<!-- update log entry 9 -->

<!-- update log entry 10 -->

<!-- update log entry 11 -->
