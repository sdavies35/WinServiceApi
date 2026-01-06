# Windows Service API Documentation

## Overview

This project is an [**ASP.NET**](http://ASP.NET) **Core Web API** designed to remotely monitor and control Windows Services. It exposes simple HTTP endpoints that allow clients—such as dashboards or automation scripts—to check if a service is running and to start or stop the service as needed. No database is required; the API acts as a thin wrapper around Windows Service management.

## Features

- Control a Windows Service (status/start/stop) via HTTP(S)
- API key authentication using `X-API-Key`
- HTTPS redirection enabled by default
- Swagger/OpenAPI with API key support
- Configurable target service name

## Requirements

- Windows host with the target service installed
- .NET 8 SDK (for development) or runtime (for hosting)
- Permissions to manage the target Windows Service (run the API with sufficient rights)
- Trusted HTTPS development certificate (`dotnet dev-certs https --trust`)

## Configuration

Update `appsettings.json` (or `appsettings.Development.json`) before running:

- **Service name** (required):

  - Set the Windows service name the API should manage.
  - Path: `ApiServiceSettings:ServiceName`
  - Example:

    ```json
    "ApiServiceSettings": {
      "ServiceName": "YourWindowsServiceName"
    }
    ```

- **API keys** (required):

  - Set allowed API keys used by the `X-API-Key` header.
  - Path: `ApiKeySettings:ValidApiKeys`
  - Example:

    ```json
    "ApiKeySettings": {
      "ValidApiKeys": [
        "your-secure-api-key-here"
      ]
    }
    ```

  - Do **not** commit real keys. For production, use environment variables or user secrets.

- **CORS** (optional):

  - Allowed origins are configured in `Program.cs` under the `RestrictedCors` policy. Add your domains there.

- **HTTPS / certificates**:

  - For local dev, trust the dev cert: `dotnet dev-certs https --trust`
  - For production, configure a valid TLS certificate (or place the API behind a reverse proxy that terminates TLS).

## Endpoints

Base path: `/Service`

- `GET /Service/GetServiceStatus` – Returns the service status.
- `POST /Service/StartService` – Starts the service and sets its start mode to **Automatic** via WMI `ChangeStartMode`.
- `POST /Service/StopService` – Stops the service and sets its start mode to **Manual** via WMI `ChangeStartMode`.

> Note: Changing start mode uses the WMI `Win32_Service.ChangeStartMode` method and requires the hosting identity to have permission to modify the service configuration.

### Example responses

- 200 OK: `{ "ServiceIsRunning": true, "StatusText": "Running" }`
- 404 Not Found: `{ "ServiceIsRunning": false, "StatusText": "Service not found." }`
- 400/500: Error details in `Message` or `StatusText`.

## Authentication

All endpoints require an API key header:

```
X-API-Key: <your-key>
```

Missing or invalid keys return 401/403.

## Getting Started

1\. Deploy the API to a Windows host where the target Windows Service is present.

2\. Ensure the host allows inbound HTTP or HTTPS traffic on the configured port.

3\. Use any HTTP client (e.g., curl, Postman, a web-based dashboard) to interact with the endpoints.

## Build & Run

```bash
# Restore & build
 dotnet restore
 dotnet build

# Run with HTTPS profile (default ports: https://localhost:7001, http://localhost:5001)
 dotnet run --launch-profile https
```

Swagger UI (with API key support) will be available at `https://localhost:7001/swagger`.

### Usage Examples

Replace `<your-key>` with a configured value and ensure `ServiceName` is set in `appsettings`.

**curl**

```bash
# Status
curl -k -H "X-API-Key: <your-key>" https://localhost:7001/Service/GetServiceStatus

# Start
curl -k -X POST -H "X-API-Key: <your-key>" https://localhost:7001/Service/StartService

# Stop
curl -k -X POST -H "X-API-Key: <your-key>" https://localhost:7001/Service/StopService
```

**PowerShell**

```powershell
# Status
Invoke-RestMethod -Method Get -Uri "https://localhost:7001/Service/GetServiceStatus" -Headers @{"X-API-Key" = "<your-key>"}

# Start
Invoke-RestMethod -Method Post -Uri "https://localhost:7001/Service/StartService" -Headers @{"X-API-Key" = "<your-key>"}

# Stop
Invoke-RestMethod -Method Post -Uri "https://localhost:7001/Service/StopService" -Headers @{"X-API-Key" = "<your-key>"}
```

## Deployment Notes

- Use strong, unique API keys; store them securely (env vars, key vault, or secrets store).
- Configure `ApiServiceSettings:ServiceName` for your environment.
- Restrict CORS origins to only the domains that need access.
- Run behind HTTPS with a trusted certificate or reverse proxy.
- In IIS, ensure the app pool identity has rights to manage the target Windows Service.

## Summary

This documentation outlines the purpose and operation of the Windows Service API. By providing three clear endpoints—`GetServiceStatus`, `StartService`, and `StopService`—clients can seamlessly integrate service monitoring and control into external tools or automation scripts without needing direct server access or custom scripts.

_Document Author: Shawn Davies_