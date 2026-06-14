# FundingMonitor

**Система мониторинга ставок финансирования криптовалютных фьючерсов**

Приложение собирает данные о ставках финансирования с бирж **Binance, Bybit, OKX**, сохраняет историю в PostgreSQL и предоставляет API для получения текущих ставок, исторических данных и арбитражных возможностей.

---

## 🚀 Быстрый старт

### Требования
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/) (для фронтенда)
- [Docker](https://www.docker.com/get-started) (опционально)

### 1. Инфраструктура (Docker)
```bash
docker-compose up -d
# PostgreSQL: localhost:5432, Redis: localhost:6379
```

### 2. Backend
```bash
dotnet restore
dotnet run --project src/FundingMonitor.Api
# Swagger UI: http://localhost:5000/ (только в Development)
```

### 3. Frontend
```bash
cd frontend/FundingMonitor.Web
npm install
npm run dev
# Frontend: http://localhost:5173
```

---

## 📚 Документация

Полная документация перемещена в папку [`docs/`](docs/):

| Раздел | Описание |
|--------|----------|
| [**Architecture**](docs/architecture/) | Диаграммы (Use Case, Sequence, Component, Deployment) |
| [**ADR**](docs/adr/) | Architecture Decision Records (8 решений) |
| [**Database**](docs/database-schema.md) | Схема БД, индексы, миграции, производительность |
| [**Deployment**](docs/deployment/) | Docker, production настройка, мониторинг |
| [**Frontend Components**](docs/frontend/components/) | Документация 7 React компонентов |
| [**Contributing**](CONTRIBUTING.md) | Workflow, code style, architecture guidelines |

---

## ✨ Ключевые возможности

- **Real-time сбор** — ставки каждые 10 секунд с 3 бирж
- **Арбитраж** — автоматический поиск разниц funding rate между биржами
- **APR аналитика** — статистика за 1д/2д/3д/7д/14д/21д/30д с кэшированием
- **История** — персистентная очередь Redis, bulk insert в PostgreSQL
- **Clean Architecture** — Core / Application / Infrastructure / Api
- **ProblemDetails (RFC 7807)** — единый формат ошибок API

---

## 🛠️ Технологический стек

| Слой | Технологии |
|------|------------|
| **Backend** | .NET 10, ASP.NET Core, EF Core 10, PostgreSQL 17, Redis 7, NLog |
| **Exchange APIs** | Binance.Net, Bybit.Net, JK.OKX.Net |
| **Frontend** | React 19, TypeScript 5.8, Vite 7, Tailwind CSS 4, Recharts 3, Axios |
| **Infra** | Docker Compose 3.8, Swagger/OpenAPI |

---

## 📊 Статус проекта

| Компонент | Статус |
|-----------|--------|
| Сбор данных (Binance, Bybit, OKX) | ✅ |
| Хранение (PostgreSQL + EF Core Bulk) | ✅ |
| API (REST + Swagger + ProblemDetails) | ✅ |
| Очередь (Redis List) | ✅ |
| Логирование (NLog, 4 файла) | ✅ |
| Frontend (React + TS + Vite) | ✅ |
| Тесты | ⏳ В планах |

---

## 📝 Лицензия

MIT

---

## 🤝 Вклад

См. [CONTRIBUTING.md](CONTRIBUTING.md) — workflow, code style, architecture guidelines.