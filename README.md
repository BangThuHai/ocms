# OCMS — Outpatient Clinic Appointment & Management System

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat&logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-Razor%20Pages-512BD4?style=flat&logo=aspnet)](https://learn.microsoft.com/aspnet/core)
[![EF Core](https://img.shields.io/badge/EF%20Core-8.0%20(Code--First)-blue?style=flat)](https://learn.microsoft.com/ef/core/)
[![SignalR](https://img.shields.io/badge/SignalR-Real--Time%20Queue-orange?style=flat&logo=signalr)](https://learn.microsoft.com/aspnet/core/signalr)
[![SQL Server](https://img.shields.io/badge/Database-SQL%20Server%202022-CC292B?style=flat&logo=microsoftsqlserver)](https://www.microsoft.com/sql-server)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

> **Course:** PRN222 — Advanced .NET Application Development  
> **Team:** SE1972-NET — **Group 6**  
> **Institution:** FPT University Hanoi  
> **System Scope:** Production-Ready Outpatient Polyclinic Workflow (4 Roles, 18 Use Cases, 17 Normalized Entities)

---

## 📖 Table of Contents
1. [Project Overview](#-project-overview)
2. [Key Architectural Highlights & Killer Features](#-key-architectural-highlights--killer-features)
3. [Technology Stack](#-technology-stack)
4. [System Architecture & Roles](#-system-architecture--roles)
5. [Database Schema (17 Entities)](#-database-schema-17-entities)
6. [Getting Started & Installation](#-getting-started--installation)
7. [Default Test Accounts](#-default-test-accounts)
8. [Project Structure](#-project-structure)
9. [Development Team](#-development-team)

---

## 🏥 Project Overview

The **Outpatient Clinic Appointment & Management System (OCMS)** is an enterprise-grade web application engineered to digitize end-to-end outpatient workflows for private polyclinics and medical centers. 

Unlike conventional CRUD booking platforms, OCMS enforces strict healthcare domain constraints, eliminating paper logbooks, crowded lobby queues, and scheduling collisions through:
* **Optimistic Concurrency Control (`RowVersion`)** preventing double-booking race conditions down to the millisecond.
* **Real-time SignalR WebSocket dispatching** streaming calling queue numbers directly to lobby monitors without polling.
* **Paperless browser-native operations** including webcam-based 2D QR check-in (`html5-qrcode`) and formatted thermal receipt printing (`window.print()`).
* **Privacy-enforced Electronic Medical Records (EMR)** safeguarding clinical diagnoses and prescription lines under role-based authorization.

---

## ⚡ Key Architectural Highlights & Killer Features

| Feature | Technical Implementation | Business Purpose |
| :--- | :--- | :--- |
| **1. Concurrency Slot Locking** | EF Core `[Timestamp]` / `RowVersion` + Database Transaction | Eliminates double-booking race conditions when multiple patients contest the same 15-minute slot. Unpaid reservations transition to `Status = 'Hold'` with a 10-minute countdown. |
| **2. Real-Time Lobby Queue Display** | ASP.NET Core SignalR `QueueHub` (`/lobbyQueueHub`) | When a doctor clicks *"Call Next"*, lobby TV monitors instantaneously display the ticket number and chime without refreshing the page. |
| **3. Webcam QR Code Check-in** | Client-side JavaScript `html5-qrcode` integration | Front-desk staff scan e-ticket QR codes directly using laptop/desktop webcams, executing intake triage validation in `< 2` seconds. |
| **4. In-Session Paraclinical Orders** | Inline Diagnostic Service Selection (`Locked UnitPrice`) | Doctors order rapid blood/urine tests, enter quantitative findings directly into the active session, and freeze prices against catalog changes. |
| **5. Consolidated Counter Settlement** | Aggregated Formula: $(\text{Consultation} + \text{Tests} + \text{Drugs}) - \text{Deposit}$ | Cashier settles balances via Cash/POS, automatically authorizes medication dispensing, and triggers browser-native thermal receipt printing. |
| **6. Atomic Pharmacy Stock Deduction** | `Medicines.StockQuantity` deduction | Dispensing staff verify paid items, hand medications over to patients, atomically decrement inventory, and print dosage instruction slips. |
| **7. Automated Hold-Release Worker** | `IHostedService` / Background Worker running on a 1-minute cron | Scans for held reservations exceeding the 10-minute window, autonomously releasing slots back to `Status = 'Open'`. |

---

## 🛠️ Technology Stack

* **Framework:** ASP.NET Core 8.0 (Razor Pages & RESTful Controllers)
* **Language:** C# 12
* **ORM:** Entity Framework Core 8.0 (Code-First Migrations)
* **Database:** Microsoft SQL Server 2022
* **Real-time Communication:** Microsoft ASP.NET Core SignalR
* **Security & Auth:** ASP.NET Core Identity (Role-Based Access Control - RBAC) with BCrypt / PBKDF2 hashing
* **Frontend UI:** Razor Syntax, HTML5, CSS3, Bootstrap 5, FontAwesome 6, Chart.js
* **Hardware Integration (Web Native):** 
  * Barcode/QR Scanning: `html5-qrcode` (Browser Webcam)
  * Slip & Invoice Printing: Native CSS `@media print` + `window.print()`

---

## 👥 System Architecture & Roles

OCMS strictly isolates permissions across **4 core user roles**:

```
                       ┌─────────────────────────┐
                       │   ASP.NET Core Identity │
                       └────────────┬────────────┘
         ┌───────────────────┬──────┴──────┬───────────────────┐
         ▼                   ▼             ▼                   ▼
    [ Patient ]         [ Doctor ]      [ Staff ]           [ Admin ]
  • Self-service      • Shift Rostering • Webcam QR Checkin • Infrastructure
  • Concurrency Book  • EMR & Diagnosis • Vital Signs Triage • Doctor Profiles
  • 10m Deposit Hold  • Rapid Tests     • SignalR Dispatch  • Shift Approval
  • Reschedule/Cancel • E-Prescriptions • Final Settlement  • Revenue Analytics
  • Feedback Ratings  • Patient Queue   • Dispense Medicine • User Accounts
```

---

## 🗄️ Database Schema (17 Entities)

The system is backed by a fully normalized (3NF) relational schema designed in Entity Framework Core:

1. **`Users`**: Central credentials, salt, password hashes, and RBAC roles (`Patient`, `Doctor`, `Staff`, `Admin`).
2. **`Patients`**: Demographic records, emergency contacts, and health insurance card references (1-to-1 with `Users`).
3. **`Doctors`**: Clinical credentials, department affiliations, assigned room references, and consultation fees (`PricePerSlot`).
4. **`Departments`**: Medical specialties governing doctor assignments and room categorizations.
5. **`Rooms`**: Physical clinic infrastructure (consultation and diagnostic examination rooms).
6. **`DoctorSchedules`**: Working shift rosters (Morning: 07:30–11:30, Afternoon: 13:00–17:00).
7. **`TimeSlots`**: Discrete 15-minute consultation slots governed by `Status (Open, Hold, Booked)` and `RowVersion` concurrency tokens.
8. **`Appointments`**: Core transactional lifecycle (`Pending`, `Confirmed`, `Arrived`, `InConsultation`, `Completed`, `Cancelled`).
9. **`VitalSigns`**: Pre-clinical triage records captured at check-in (Blood Pressure, Heart Rate, SpO2, Temperature, Weight, Height, BMI).
10. **`MedicalRecords`**: Finalized clinical consultations, ICD-10 diagnoses, confidential notes, and recall dates.
11. **`MedicalServices`**: Master catalog of outpatient rapid diagnostic tests and ultrasound/imaging services.
12. **`ServiceOrders`**: Line items for ordered diagnostic tests preserving historical frozen `UnitPrice` and clinical findings.
13. **`Medicines`**: Outpatient pharmacy formulary maintaining unit prices and real-time inventory balances (`StockQuantity`).
14. **`PrescriptionDetails`**: Line items per medical record capturing prescribed quantities, frozen prices, and dosage instructions.
15. **`Payments`**: Financial transactions (`Deposit`, `FinalSettlement`, `Refund`) with statuses (`Pending`, `Success`, `Failed`, `Refunded`).
16. **`Feedbacks`**: Post-visit patient satisfaction ratings (1–5 stars) and qualitative feedback comments.
17. **`Notifications`**: Audit logs of automated dispatch reminders and transactional verification codes.

---

## 🚀 Getting Started & Installation

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or higher
* [Microsoft SQL Server 2019/2022](https://www.microsoft.com/sql-server) (or SQL Server Express / LocalDB)
* [Visual Studio 2022](https://visualstudio.microsoft.com/) (v17.8+) or [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit

### 1. Clone the Repository
```bash
git clone https://github.com/your-username/OCMS_PRN222_Group6.git
cd OCMS_PRN222_Group6
```

### 2. Configure Database Connection String
Open `src/OCMS.Web/appsettings.json` and adjust the connection string to target your SQL Server instance:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OCMS_Db;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

### 3. Apply Migrations & Seed Default Data
Run the following commands in the Package Manager Console or terminal to create the database schema and seed master catalogs:
```bash
dotnet ef database update --project src/OCMS.Infrastructure --startup-project src/OCMS.Web
```

### 4. Run the Application
```bash
dotnet run --project src/OCMS.Web
```
Navigate to `https://localhost:5001` or `http://localhost:5000` in your web browser.

---

## 🔑 Default Test Accounts

All accounts come pre-configured via database seeding for instant testing:

| Role | Username / Email | Password | Access Scope |
| :--- | :--- | :--- | :--- |
| **System Admin** | `admin@ocms.clinic` | `Admin@123` | Clinic settings, user accounts, shift approval, revenue dashboards |
| **Clinic Staff** | `staff@ocms.clinic` | `Staff@123` | Webcam QR check-in, vital signs triage, counter settlement, pharmacy dispensing |
| **Doctor** | `doctor.an@ocms.clinic` | `Doctor@123` | Shift scheduling, daily room queue, clinical EMR, test ordering, e-prescriptions |
| **Patient** | `patient.phong@gmail.com` | `Patient@123` | Online doctor search, slot reservation, e-ticket QR pass, booking history |

---

## 📂 Project Structure

```text
OCMS/
├── src/
│   ├── OCMS.Core/                     # Domain Entities, Enums, Interfaces, Value Objects
│   │   ├── Entities/                  # 17 normalized domain model classes
│   │   ├── Enums/                     # Status, ShiftType, Role definitions
│   │   └── Interfaces/                # Core repository & unit of work contracts
│   │
│   ├── OCMS.Infrastructure/           # Data Access & External Integrations
│   │   ├── Data/                      # OcmsDbContext, Fluent API Configurations
│   │   ├── Migrations/                # EF Core Code-First Migrations
│   │   ├── Repositories/              # Generic & Entity-specific Repositories
│   │   └── Services/                  # Background Worker (HoldReleaseWorker)
│   │
│   ├── OCMS.Application/              # Application Business Logic & DTOs
│   │   ├── DTOs/                      # Request / Response data transfer objects
│   │   ├── Services/                  # BookingService, EmrService, BillingService
│   │   └── Hubs/                      # QueueHub (SignalR WebSocket implementation)
│   │
│   └── OCMS.Web/                      # Presentation Layer (Razor Pages & Controllers)
│       ├── Pages/                     # Role-partitioned Razor Pages
│       │   ├── Patient/               # Booking catalog, appointment history
│       │   ├── Doctor/                # Patient queue, EMR workspace, shift registration
│       │   ├── Staff/                 # QR check-in, triage entry, counter billing, pharmacy
│       │   └── Admin/                 # Specialty/Room config, analytics, user management
│       ├── wwwroot/                   # Static assets, CSS, Chart.js, html5-qrcode
│       ├── Program.cs                 # DI container, middleware pipeline, SignalR routes
│       └── appsettings.json           # Connection strings, application config
│
├── docs/                              # Project documentation, SRS, architecture diagrams
└── README.md
```

---

## 👨‍💻 Development Team

**SE1972-NET — Group 6 (FPT University Hanoi)**

| Student ID | Full Name | Project Role | Core Responsibilities |
| :---: | :--- | :---: | :--- |
| **HE190966** | **Trần Quang Phong** | **Team Leader / Architect** | Clean Architecture, Concurrency Booking Engine (`RowVersion`), Database Migrations, Project Coordination |
| **HE191710** | **Lê Đức Anh** | **Core Developer** | Doctor Shift Rostering, Clinical Care Workspace, EMR ICD-10 Tagging, Paraclinical Test Ordering |
| **HE191558** | **Nguyễn Hữu Minh Đức** | **Core Developer** | Front-Desk Webcam QR Check-in (`html5-qrcode`), Triage Vital Signs Recording, SignalR Queue Hub (`/LobbyQueue`) |
| **HE190879** | **Nguyễn Xuân Bằng** | **Core Developer** | Cashier Consolidated Counter Settlement, Receipt Printing (`window.print()`), Pharmacy Dispensing & Stock Deduction |
| **HE191827** | **Nguyễn Việt Hà** | **Core Developer** | System Administration, Department & Consultation Room Management, Chart.js Revenue & Shift Analytics, Background Workers |

---

## 📄 License
This project is developed for educational purposes under the **PRN222** curriculum at FPT University. Distributed under the MIT License.
