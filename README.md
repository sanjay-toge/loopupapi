# LoopUpAPI

**LoopUpAPI** is a .NET-based web API project designed to provide core functionalities such as user management, geolocation, and secure authentication. It supports querying nearby users via MongoDB’s geospatial features.

---

##  Project Structure

- **Controllers/** – HTTP endpoints for API operations.
- **DTOs/** – Data Transfer Objects to shape request and response payloads.
- **Models/** – Domain classes (e.g., `User`) with MongoDB attributes and GeoJSON types.
- **Services/** – Business logic layer handling data operations and validations.
- **Mappers/** – Converters between domain models and DTOs.
- **Helpers/** & **Extensions/** – Utility classes and extension methods.
- **config/** – Environment-specific settings (e.g., `appsettings.json`, secrets).
- **Program.cs** – Entry point and DI setup.
- `LoopUpAPI.http` – Example HTTP client for testing API endpoints.

---

##  Features & Capabilities

- **Geo-based user location** → Stores `location` as MongoDB-compatible GeoJSON (`Point`) and supports proximity queries.
- **Automatic Indexing** → Ensures `2dsphere` index on startup for efficient geospatial queries.
- **User CRUD Operations** → Create, update, retrieve, and delete user profiles.
- **Nearby Users API** → Fetch other users within a specified radius from your location.
- **Configurable via appsettings** → Easily define database connection, authentication, logging, etc.

---

##  Getting Started

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com)
- [MongoDB](https://www.mongodb.com) instance (local or hosted)
- IDE: Visual Studio, VS Code, or any C# editor of your choice

### Setup

1. **Clone the repo**
   ```bash
   git clone https://github.com/sanjay-toge/loopupapi.git
   cd loopupapi






"MongoSettings": {
  "ConnectionString": "mongodb://localhost:27017",
  "DatabaseName": "LoopUpDB"
}


Run the API

Using VS Code / CLI

dotnet build
dotnet run
