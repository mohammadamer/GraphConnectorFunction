# GraphConnectorFunction
GraphConnectorFunction is Azure Function App for creating connection, schema, load and ingest M365 roadmap features to M365

## Overview

This Project contains a Microsoft Graph connector Azure Function app that demonstrates how to Create connection, load and ingest M365 Roadmap content in Microsoft 365 using Microsoft Graph connectors. The ingested content is set to be visible to everyone in the organization.


## Contributors

- [Mohammad Amer](https://github.com/mohammadamer)

## Version history

Version|Date|Comments
-------|----|--------
1.0|May 29, 2025|Initial release

## Prerequisites

- [Microsoft 365 Developer tenant](https://developer.microsoft.com/microsoft-365/dev-program)
- [.NET 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- Created and Configure Microsoft Entra app registration. Grant it the following API permissions
  - ExternalConnection.ReadWrite.OwnedBy
  - ExternalItem.ReadWrite.OwnedBy

## Minimal path to awesome

- Clone this repository 
- add the information about the Entra ID app in user secrets
  - "TenantID": "Tenant-id",
  - "ClientId": "client-id",
  - "ClientSecret": "client-secret"

- Build the project: `dotnet build`
- Run Azure Function app then trigger CreateConnection function

## Features
This sample shows how to Ingest M365 Roadmap content in Microsoft 365 using Microsoft Graph connectors using C# and .NET

