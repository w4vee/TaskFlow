# TaskFlow - Full-stack Kanban Board

A modern full-stack Kanban board application built with .NET 8 and React 19.

## Features

- User Authentication (Register, Login, JWT)
- Board Management (CRUD)
- Task Management (CRUD, Status Updates)
- Kanban Board UI
- RESTful API with Swagger
- Unit Tests

## Tech Stack

**Backend:** .NET 8, Clean Architecture, CQRS + MediatR, EF Core, FluentValidation, AutoMapper, JWT

**Frontend:** React 19, TypeScript, Vite, Tailwind CSS, React Router, Axios

## Project Structure

```
TaskFlow/
├── taskflow-be/              # Backend (.NET 8)
│   ├── TaskFlow.API/         # Web API layer
│   ├── TaskFlow.Application/ # Use cases, CQRS
│   ├── TaskFlow.Domain/     # Entities, interfaces
│   ├── TaskFlow.Infrastructure/ # Data access, services
│   └── TaskFlow.Tests/      # Unit tests
│
├── taskflow-ui/              # Frontend (React)
│   ├── src/
│   │   ├── api/             # API client
│   │   ├── components/      # UI components
│   │   ├── contexts/        # React contexts
│   │   ├── pages/           # Page components
│   │   └── types/           # TypeScript types
│
└── docker-compose.yml        # Docker orchestration
```

```
