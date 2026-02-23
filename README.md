# WalletFlow

Production-grade microservices platform for digital wallet management built with .NET 8.

## Architecture
`
Client
  |
API Gateway (YARP) :5000
  |              |
Identity :5001   Finance :5002
  |              |
  +----RabbitMQ--+
  |              |
PostgreSQL     Redis
`

## Tech Stack

- .NET 8, ASP.NET Core Minimal APIs
- Clean Architecture + DDD + CQRS + Event Sourcing + Hexagonal
- MassTransit 8 + RabbitMQ
- SignalR real-time notifications
- YARP Reverse Proxy
- Entity Framework Core 7 + PostgreSQL 16
- Redis 7 cache
- JWT Bearer + Refresh Tokens + BCrypt
- Outbox Pattern, Repository, Mediator
- Docker + Docker Compose

## Quick Start
`ash
git clone https://github.com/Vladimir-Bulan/WalletFlow-.git
cd WalletFlow
docker-compose up -d
`

Ports:
- Gateway: http://localhost:5000
- Identity: http://localhost:5001
- Finance:  http://localhost:5002
- RabbitMQ: http://localhost:15672 (guest/guest)

## API Examples

### Register
`ash
curl -X POST http://localhost:5000/api/auth/register   -H "Content-Type: application/json"   -d '{"email":"john@example.com","firstName":"John","lastName":"Doe","password":"Password123!"}'
`

### Login
`ash
curl -X POST http://localhost:5000/api/auth/login   -H "Content-Type: application/json"   -d '{"email":"john@example.com","password":"Password123!"}'
`

### Deposit
`ash
curl -X POST http://localhost:5000/api/accounts/{id}/deposit   -H "Authorization: Bearer {token}"   -H "Content-Type: application/json"   -d '{"amount": 1000}'
`

### Health Checks
`ash
curl http://localhost:5001/health
curl http://localhost:5002/health/ready
`

## Key Patterns

- **Outbox Pattern** - Guaranteed at-least-once event delivery
- **Event Sourcing** - Immutable event log, state rebuilt by replaying
- **CQRS** - Commands and queries separated via MediatR
- **DDD** - Value Objects, Aggregates, Domain Events

## Tests
`ash
dotnet test
`
