# WalletFlow

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET-Core-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![Redis](https://img.shields.io/badge/Redis-7-red)
![RabbitMQ](https://img.shields.io/badge/RabbitMQ-Message%20Broker-orange)
![Docker](https://img.shields.io/badge/Docker-Containerized-2496ED)
![Architecture](https://img.shields.io/badge/Architecture-Microservices-black)
![Pattern](https://img.shields.io/badge/Pattern-CQRS%20%7C%20DDD%20%7C%20Event%20Sourcing-success)

Production-grade **microservices platform** for digital wallet management built with **.NET 8**, applying modern distributed systems and enterprise architecture patterns.

---

## 🚀 Overview

WalletFlow is a backend-focused distributed system designed to demonstrate:

- Microservices architecture
- Clean Architecture + DDD
- CQRS + Event Sourcing
- Reliable messaging with Outbox Pattern
- Real-time notifications with SignalR
- Production-ready Dockerized environment

This project showcases enterprise-level backend engineering practices.

---

## 🏗 Architecture

```
                    ┌──────────────┐
                    │    Client    │
                    └───────┬──────┘
                            │
                    ┌───────▼────────┐
                    │  API Gateway   │
                    │    (YARP)      │
                    │     :5000      │
                    └───────┬────────┘
               ┌────────────┴────────────┐
               │                         │
        ┌──────▼───────┐         ┌──────▼───────┐
        │   Identity   │         │   Finance    │
        │     :5001    │         │     :5002    │
        └──────┬───────┘         └──────┬───────┘
               │                         │
               └───────────┬─────────────┘
                           │
                     ┌─────▼─────┐
                     │ RabbitMQ  │
                     └─────┬─────┘
                           │
                ┌──────────┴──────────┐
                │                     │
          ┌─────▼─────┐         ┌─────▼─────┐
          │ PostgreSQL│         │   Redis   │
          └───────────┘         └───────────┘
```

---

## 🧱 Tech Stack

### Core
- .NET 8
- ASP.NET Core Minimal APIs
- Clean Architecture
- Domain-Driven Design (DDD)
- CQRS (MediatR)
- Event Sourcing

### Messaging
- MassTransit 8
- RabbitMQ
- Outbox Pattern

### Data
- Entity Framework Core 7
- PostgreSQL 16
- Redis 7 (Distributed Cache)

### Security
- JWT Bearer Authentication
- Refresh Tokens
- BCrypt password hashing

### Infrastructure
- YARP Reverse Proxy
- SignalR (real-time notifications)
- Docker
- Docker Compose

---

## 🔐 Core Features

- User registration & authentication
- JWT access + refresh tokens
- Account management
- Deposit transactions
- Event-driven communication
- Real-time notifications
- Health checks & readiness endpoints
- Distributed caching with Redis

---

## ⚙️ Quick Start

```bash
git clone https://github.com/Vladimir-Bulan/WalletFlow-.git
cd WalletFlow
docker-compose up -d
```

---

## 🌐 Services & Ports

| Service      | URL |
|--------------|------|
| Gateway      | http://localhost:5000 |
| Identity     | http://localhost:5001 |
| Finance      | http://localhost:5002 |
| RabbitMQ UI  | http://localhost:15672 (guest/guest) |

---

## 🧪 API Examples

### Register

```bash
curl -X POST http://localhost:5000/api/auth/register \
-H "Content-Type: application/json" \
-d '{"email":"john@example.com","firstName":"John","lastName":"Doe","password":"Password123!"}'
```

### Login

```bash
curl -X POST http://localhost:5000/api/auth/login \
-H "Content-Type: application/json" \
-d '{"email":"john@example.com","password":"Password123!"}'
```

### Deposit

```bash
curl -X POST http://localhost:5000/api/accounts/{id}/deposit \
-H "Authorization: Bearer {token}" \
-H "Content-Type: application/json" \
-d '{"amount": 1000}'
```

### Health Checks

```bash
curl http://localhost:5001/health
curl http://localhost:5002/health/ready
```

---

## 🧠 Architectural Patterns

### Outbox Pattern
Ensures reliable at-least-once event delivery by persisting integration events in the same transaction as domain changes.

### Event Sourcing
Application state is reconstructed by replaying immutable domain events.

### CQRS
Commands and Queries are separated to improve scalability, maintainability, and read/write optimization.

### Domain-Driven Design
- Aggregates  
- Value Objects  
- Domain Events  
- Bounded Contexts  

---

## 🧪 Running Tests

```bash
dotnet test
```

---

## 📦 Future Improvements

- Kubernetes deployment
- OpenTelemetry integration
- Prometheus + Grafana monitoring
- Distributed tracing
- Saga orchestration
- Rate limiting
- API versioning
- Load testing

---

## 👨‍💻 Author

Vladimir Bulan  
Backend Developer | .NET | Distributed Systems  