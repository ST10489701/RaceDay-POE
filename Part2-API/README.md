# RaceDay API – Part 2

## Project Overview

RaceDay API is a web API for managing race events, event categories, participant enrolments, routes, and race results. It is developed using C# and ASP.NET Core with Entity Framework Core and SQL Server.

## Technologies Used

- C#
- ASP.NET Core Web API
- .NET 8
- Entity Framework Core
- Microsoft SQL Server
- xUnit for automated testing
- GitHub Actions for continuous integration

## User Roles

### 1. Organiser

The Organiser can:
- Create, update, and delete their own events.
- Manage event categories.
- View enrolments for their own events.
- Capture race results.

### 2. Participant

The Participant can:
- Enrol in event categories.
- View and cancel their own enrolments.
- View their own race results.
- Manage their own profile.

Both roles can view public event, category, and route information.

## Prerequisites

Install the .NET 8 SDK and configure a supported Microsoft SQL Server database. Visual Studio or Visual Studio Code can be used for development.

## Setup and Run Instructions

1. Clone or download this repository.
2. Open a terminal in the `Part2-API` directory.
3. Restore the project dependencies:

   `dotnet restore RaceDay.sln`

4. Build the solution:

   `dotnet build RaceDay.sln`

5. Configure the `RaceDay` connection string in `RaceDay.Api/appsettings.json` to point to your SQL Server database.

6. Apply the Entity Framework Core migrations using the database configuration supported by your environment.

7. Run the API:

   `dotnet run --project RaceDay.Api`

8. Open the Swagger URL displayed in the terminal to explore and test the API endpoints.

**Note:** LocalDB is intended for supported Windows environments and is not supported in Linux Codespaces. Configure a supported SQL Server instance before running the API in Codespaces.

## Running the Tests

From the `Part2-API` directory, run:

`dotnet test RaceDay.sln`

This runs the automated tests included in the solution.

## Continuous Integration (CI)

The GitHub Actions workflow is located at `.github/workflows/ci.yml`. It is configured to restore, build, and test the solution when the configured GitHub events occur.

A screenshot of a successful workflow run should be included as evidence for the assignment.

## Repository Structure

- `RaceDay.Api` – ASP.NET Core Web API application.
- `RaceDay.Api.Tests` – Automated test project.
- `RaceDay.sln` – Solution file.
- `.github/workflows/ci.yml` – GitHub Actions CI workflow.
- `README.md` – Project documentation.

## Author

RaceDay Part 2 API assignment.

