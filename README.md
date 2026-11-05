# MediDrop

**Medication Management and Automated Dispensing System**

MediDrop is a medication management and automated dispensing system developed as an academic software engineering project.

The system combines an ASP.NET Core MVC web application with an ESP32-S3-based physical medication dispenser. It provides a central platform for managing medication schedules, prescriptions, dispensing activity and medication adherence.

The web application is hosted on Microsoft Azure, while the physical dispenser communicates with the application through REST API endpoints.

## Technology Stack

### Web Application

- ASP.NET Core MVC — C#
- Entity Framework Core
- ASP.NET Core Identity
- Microsoft SQL Server / Azure SQL Database
- HTML, CSS, JavaScript and Bootstrap

### Cloud Services

- Azure App Service — Web application hosting
- Azure SQL Database — Application data storage
- Azure Blob Storage — Medication image storage

### Dispenser Firmware

- ESP32-S3
- C++ / Arduino Framework
- PlatformIO
- Wi-Fi and REST API communication

## System Features

### Patient Portal

The patient portal provides functionality for managing medication and interacting with the dispenser.

- Medication schedule management
- Personal medication inventory
- Prescription viewing and collection
- Dispenser pairing and management
- Remote dispensing and device commands
- Dispensing history and medication adherence tracking
- Notifications and care provider relationships

### Care Provider Portal

The care provider portal allows verified healthcare providers to manage patient-related medication activities.

- Patient relationship management
- Patient medication adherence monitoring
- Prescription creation and management
- Medication catalogue submissions
- Care provider account and verification management

### Administration Portal

The administration portal provides system management and approval functionality.

- Care provider registration verification
- Medication catalogue approval
- Administrative management features

### Automated Dispenser

The ESP32-based dispenser integrates with the web application to perform scheduled medication dispensing.

- Supports 21 physical medication compartments arranged in a 7 × 3 configuration
- Downloads medication schedules from the web application
- Performs scheduled and authorised early dispensing
- Supports dispensing confirmation and cancellation
- Reports dispensing and medication collection events
- Provides device status and operational feedback
- Supports remote commands, including homing and purging
- Uses a local schedule to continue dispensing during temporary connectivity interruptions

## System Architecture

MediDrop consists of two primary components.

**1. ASP.NET Core MVC Application**

The web application provides the user interface, authentication, medication management, scheduling and backend API.

It uses ASP.NET Core Identity for user authentication and role-based access control.

Application data is stored in Azure SQL Database, while medication images are stored in Azure Blob Storage.

**2. ESP32 Dispenser Firmware**

The dispenser operates as a separate embedded application.

It communicates with the web application's REST API to:

- Retrieve medication schedules and schedule updates
- Poll for authorised device commands
- Report device status and heartbeats
- Submit dispensing and medication collection events

The dispenser maintains its active schedule locally to support temporary network interruptions.

## Repository Structure

```text
MediDropProject/
├── Controllers/          # MVC and API controllers
├── Data/                 # Database context and configuration
├── Domain/               # Domain entities and enumerations
├── MediDrop.Firmware/    # ESP32 PlatformIO firmware
├── Migrations/           # Entity Framework Core migrations
├── Models/               # Application and view models
├── Properties/           # Local development launch settings
├── Services/             # Application services
├── ViewComponents/       # Reusable Razor view components
├── Views/                # Razor views
├── wwwroot/              # CSS, JavaScript, images and libraries
├── appsettings.json      # Application configuration
├── Program.cs            # Web application entry point
└── MediDropProject.sln   # .NET solution
```

The web application and dispenser firmware are maintained in the same repository but are built and deployed independently.

## Deployment and Configuration

### Web Application

The MediDrop web application is currently configured for deployment to Microsoft Azure and is not set up for standalone local execution.

The deployed application depends on:

- Azure App Service
- Azure SQL Database
- Azure Blob Storage
- Environment-specific application configuration

Database schema changes are managed using Entity Framework Core migrations.

Production credentials, connection strings, Azure resources and database contents are not included in this repository.

### Dispenser Firmware

**Requirements:**

- ESP32-S3 development board
- Visual Studio Code with PlatformIO
- Compatible medication dispenser hardware
- Wi-Fi connectivity

**Firmware setup:**

1. Open the `MediDrop.Firmware` directory in Visual Studio Code.
2. Ensure the PlatformIO extension is installed.
3. Configure the required Wi-Fi and API connection settings.
4. Build and upload the firmware to the ESP32-S3.
5. Pair the dispenser with a patient account through the deployed web application.

The firmware requires the corresponding physical hardware to perform dispensing operations.

## Security

MediDrop implements authentication and access restrictions through ASP.NET Core Identity and role-based authorisation.

The dispenser uses device-specific credentials to authenticate requests to the backend API.

Sensitive configuration values, production credentials and database connection strings must be managed separately from the source code.

## Project Scope

MediDrop was developed as an academic prototype to demonstrate the integration of:

- Web application development
- Relational database management
- Cloud hosting and services
- Embedded systems programming
- REST API communication
- Automated hardware control
