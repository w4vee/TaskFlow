# TaskFlow - Full-stack Kanban Board

A modern full-stack Kanban board application built with .NET 8, React 19, and Clean Architecture.

## 🚀 Features

- **User Authentication**: Register, Login, JWT-based authentication with refresh tokens
- **Board Management**: Create, Read, Update, Delete boards
- **Task Management**: Full CRUD operations with status updates
- **Kanban Board**: Visual board with drag-and-drop columns
- **RESTful API**: Clean API design with OpenAPI/Swagger documentation
- **Unit Tests**: Comprehensive test coverage with xUnit, Moq, and FluentAssertions

## 🛠 Tech Stack

### Backend
| Technology | Description |
|------------|-------------|
| .NET 8 | Modern .NET framework |
| Clean Architecture | Domain-Driven Design layers |
| CQRS + MediatR | Command Query Responsibility Segregation |
| Entity Framework Core | ORM with SQL Server |
| FluentValidation | Input validation |
| AutoMapper | Object mapping |
| JWT Authentication | Secure API access |

### Frontend
| Technology | Description |
|------------|-------------|
| React 19 | UI library |
| TypeScript | Type safety |
| Vite | Build tool |
| Tailwind CSS | Styling |
| React Router | Navigation |
| Axios | HTTP client |

## 📁 Project Structure

```
TaskFlow/
├── taskflow-be/              # Backend (.NET 8)
│   ├── TaskFlow.API/        # Web API layer
│   ├── TaskFlow.Application/ # Use cases, CQRS
│   ├── TaskFlow.Domain/     # Entities, interfaces
│   ├── TaskFlow.Infrastructure/ # Data access, services
│   └── TaskFlow.Tests/     # Unit tests
│
├── taskflow-ui/             # Frontend (React)
│   ├── src/
│   │   ├── api/            # API client
│   │   ├── components/    # UI components
│   │   ├── contexts/      # React contexts
│   │   ├── pages/          # Page components
│   │   └── types/          # TypeScript types
│   └── package.json
│
└── docker-compose.yml       # Docker orchestration
```

## 🐳 Docker

### Prerequisites
- Docker Desktop
- Docker Compose

### Run with Docker

```bash
# Build and run all services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

Services will be available at:
- **Backend API**: http://localhost:5000
- **Frontend**: http://localhost:3000
- **SQL Server**: localhost:1433

## 💻 Local Development

### Backend

```bash
cd taskflow-be

# Restore packages
dotnet restore

# Run migrations (update connection string in appsettings.json first)
dotnet ef database update

# Run the API
dotnet run --project TaskFlow.API
```

### Frontend

```bash
cd taskflow-ui

# Install dependencies
npm install

# Run development server
npm run dev
```

## 🧪 Testing

```bash
# Run backend tests
cd taskflow-be
dotnet test
```

## 📝 API Documentation

Once the API is running, visit:
- Swagger UI: http://localhost:5000/swagger

## 🎯 Learning Outcomes

This project demonstrates:
- Clean Architecture with proper separation of concerns
- CQRS pattern with MediatR
- JWT authentication flow
- Entity Framework Core with migrations
- React with modern hooks and context
- Docker containerization
- Unit testing best practices

## 📄 License

MIT License
