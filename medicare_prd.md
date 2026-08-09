# Medicare Project - Product Requirements Document (PRD)

## 1. Project Overview
**Medicare** is a comprehensive, full-stack healthcare platform designed to seamlessly connect patients, doctors, and administrators. The application enables patients to browse and book both individual doctors and specialized medical services. It provides doctors with a portal to manage their schedule and appointments, while administrators have a dedicated dashboard to oversee users, services, doctors, and platform analytics.

The platform relies on a modular architecture divided into three distinct projects:
- **Frontend (Patient Portal)**: A React-based web app for patients to discover services/doctors and book appointments.
- **Admin Dashboard**: A separate React-based control panel for platform administrators to manage inventory, staff, and appointments.
- **Backend API**: A robust .NET Core API that serves both frontends, manages the PostgreSQL database, and handles authentication and business logic.

---

## 2. Technologies and Tools Used

### Backend (.NET)
- **Framework**: ASP.NET Core (Minimal APIs & MVC structure).
- **ORM**: Entity Framework Core (EF Core).
- **Database**: PostgreSQL (utilizing advanced PostgreSQL features like `jsonb` for dynamic scheduling and `text[]` arrays).
- **Authentication**: Custom JWT (JSON Web Tokens) with optional Clerk Identity integration.
- **Cloud Storage**: Cloudinary (via `_imageUploadService` for handling image uploads for services, doctors, and profiles).

### Frontend (Patient & Admin)
- **Framework**: React.js (via Vite for fast compilation).
- **Routing**: React Router DOM.
- **Styling**: Tailwind CSS (with custom utility classes and design tokens).
- **Icons**: Lucide React.
- **State Management**: React Context API (e.g., `AuthContext`).

---

## 3. Database Design & Tables

The system uses Entity Framework Core to map C# domain models to PostgreSQL tables. It heavily relies on relational foreign keys and cascading deletes (or soft deletes) to maintain data integrity.

### 1. `Users`
The central identity table for all individuals on the platform.
- **Purpose**: Handles authentication credentials, basic demographics, and role-based access control.
- **Key Columns**: `Id`, `Email`, `PasswordHash`, `Name`, `Role` (Enum: PATIENT, DOCTOR, ADMIN), `ClerkId`.
- **Relationships**: 1:1 with `Patient`, 1:1 with `Doctor`, 1:N with `UserSession`, 1:N with `Appointment` & `ServiceAppointment`.

### 2. `UserSessions`
- **Purpose**: Stores active session tokens for secure logout and token invalidation.
- **Key Columns**: `Id`, `UserId`, `Token`, `ExpiresAt`.
- **Relationships**: N:1 with `Users`.

### 3. `Doctors`
- **Purpose**: Extended profile information for users with the `DOCTOR` role.
- **Key Columns**: `Id`, `UserId`, `Specialization`, `Experience`, `Fee`, `Schedule` (JSONB column for complex availability arrays), `IsDeleted` (Soft delete flag).
- **Relationships**: 1:1 with `Users`, 1:N with `Appointments`.

### 4. `Patients`
- **Purpose**: Extended medical profile information for users with the `PATIENT` role.
- **Key Columns**: `Id`, `UserId`, `BloodGroup`, `MedicalHistory`, `Allergies`, `EmergencyContactName`.
- **Relationships**: 1:1 with `Users`.

### 5. `Services`
- **Purpose**: Defines medical services or health checkups that patients can book independently of a specific doctor (e.g., "Full Body Checkup").
- **Key Columns**: `Id`, `Name`, `Price`, `Slots` (JSONB), `IsDeleted` (Soft delete flag).
- **Relationships**: 1:N with `ServiceAppointments`.

### 6. `Appointments`
- **Purpose**: Tracks bookings made by Patients specifically for Doctors.
- **Key Columns**: `Id`, `UserId`, `DoctorId`, `Date`, `TimeSlot`, `Status` (Enum: PENDING, CONFIRMED, CANCELED), `PaymentStatus`.
- **Relationships**: N:1 with `Users`, N:1 with `Doctors`.

### 7. `ServiceAppointments`
- **Purpose**: Tracks bookings made by Patients for specific Services.
- **Key Columns**: `Id`, `UserId`, `ServiceId`, `Date`, `TimeSlot`, `Status`.
- **Relationships**: N:1 with `Users`, N:1 with `Services`.

> **Note on Deletions**: Both `Service` and `Doctor` tables implement a **Soft Delete** pattern (`IsDeleted = true`). EF Core uses Global Query Filters (`modelBuilder.Entity<Service>().HasQueryFilter(s => !s.IsDeleted)`) to automatically hide deleted records while preserving historical appointment data and foreign key constraints.

---

## 4. Architecture & Project Structure

The project utilizes a decoupled client-server architecture:

```text
MEDICARE/
├── admin/               # React SPA (Admin Dashboard)
│   ├── src/pages/       # Admin views (e.g., ListServicePage, AdminProfile)
│   ├── src/components/  # Reusable UI components
│   └── src/assets/      # Styles (e.g., dummyStyles.js)
├── frontend/            # React SPA (Patient Portal)
│   ├── src/pages/       # Patient views (e.g., Home, DoctorListing)
│   └── src/components/  # Reusable UI components (e.g., Navbar)
└── backend-dotnet/      # ASP.NET Core API
    ├── Controllers/     # MVC Controllers (or Endpoints for Minimal APIs)
    ├── Models/          # DTOs (Data Transfer Objects) & Enums
    │   └── Domain/      # EF Core Entity Models
    ├── Data/            # ApplicationDbContext (EF Core)
    ├── Services/        # Business Logic Services (e.g., IUserService)
    └── Program.cs       # DI Container & App Configuration
```

---

## 5. Authentication Flow

1. **Login/Registration**: The user submits credentials to the backend (`/api/auth/login` or `/api/auth/register`).
2. **JWT Generation**: The backend validates credentials and generates a secure JSON Web Token (JWT).
3. **Session Creation**: The token is stored in the `UserSessions` table to allow for backend invalidation (secure sign-out).
4. **Client Storage**: The frontend stores the token in `localStorage` (as `authToken`).
5. **Authenticated Requests**: Every protected API call from the React apps includes the token in the `Authorization: Bearer <token>` header.
6. **Role Authorization**: The backend inspects the JWT payload to determine the `Role` (Admin vs Patient) and grants or denies access to specific endpoints.

---

## 6. Core Business Logic & APIs

### User & Auth APIs (`/api/user`, `/api/auth`)
- Manages registration for distinct roles (`register-admin`, `register-patient`).
- Profile fetching and updating (e.g., `GetAdminProfileAsync`, `UpdatePatientProfileAsync`).

### Doctor APIs (`/api/doctors`)
- Allows admins to onboard new doctors.
- Patients can fetch available doctors and view their JSONB parsed schedules.
- `DELETE /api/doctors/DeleteDoctor/{id}` triggers a soft delete on the doctor entity.

### Service APIs (`/api/services`)
- Full CRUD for medical services. Services support image uploads via Cloudinary.
- `DELETE /api/services/DeleteService/{id}` triggers a soft delete on the service entity.

### Appointment APIs (`/api/appointments`, `/api/service-appointments`)
- Checks for slot availability before booking to prevent double-booking.
- Handles status transitions (Pending -> Confirmed -> Completed/Canceled).
- Calculates analytical data (e.g., Total Earnings, Completed appointments) for the Admin Service Dashboard.
