# TaskFlow - Full-stack Kanban Board

A modern full-stack Kanban board application built with .NET 8 and React 19.

## Features

- User Authentication (Register, Login, JWT)
- Board Management (CRUD)
- Task Management (CRUD, Status Updates)
- Kanban Board UI
- RESTful API with Swagger

## Tech Stack

**Backend:** .NET 8, Clean Architecture, CQRS + MediatR, EF Core, FluentValidation, AutoMapper, JWT

**Frontend:** React 19, TypeScript, Vite, Tailwind CSS, React Router, Axios

## Project Structure

```
taskflow-be/          # Backend (.NET 8)
taskflow-ui/         # Frontend (React)
docker-compose.yml   # Docker orchestration
```

## Quick Start

### With Docker
```bash
docker-compose up -d
```

### Manual Setup

**Backend:**
```bash
cd taskflow-be
dotnet restore
dotnet run --project TaskFlow.API
```

**Frontend:**
```bash
cd taskflow-ui
npm install
npm run dev
```

## Testing
```bash
cd taskflow-be
dotnet test
```
