# FundingMonitor Documentation

## 📚 Documentation Structure

| Категория | Описание | Файлы |
|-----------|----------|-------|
| **Getting Started** | Быстрый старт, требования, запуск | [README.md](../README.md) |
| **Architecture** | Архитектурные решения, диаграммы | [architecture/](architecture/) |
| **API Reference** | OpenAPI/Swagger (автогенерируется) | Swagger UI: `/` (Development) |
| **Database** | Схема БД, миграции, производительность | [database-schema.md](database-schema.md) |
| **Deployment** | Docker, production настройка | [deployment/](deployment/) |
| **Frontend** | React компоненты, хуки, типы | [frontend/](frontend/) |
| **Development** | Contributing, code style, workflow | [CONTRIBUTING.md](../CONTRIBUTING.md) |
| **ADR** | Architecture Decision Records | [adr/](adr/) |

---

## 🏗️ Architecture Documentation

### Diagrams
- [Use Case & Sequence Diagrams](architecture/USE_CASE_DIAGRAM.md) — PlantUML / Mermaid диаграммы вариантов использования, последовательности, компоненты, развёртывание

### Architecture Decision Records (ADR)
- [ADR Index](adr/index.md) — список всех решений
- [0001: Clean Architecture Layers](adr/0001-clean-architecture-layers.md)
- [0002: Background Services for Data Collection](adr/0002-background-services-for-data-collection.md)
- [0003: Redis Queue for Historical Collection](adr/0003-redis-queue-for-historical-collection.md)
- [0004: Entity Framework Core with PostgreSQL](adr/0004-entity-framework-core-with-postgresql.md)
- [0005: ProblemDetails for Error Handling](adr/0005-problem-details-for-error-handling.md)
- [0006: Multi-Exchange Support with Abstraction](adr/0006-multi-exchange-support-with-abstraction.md)
- [0007: APR Calculation Methodology](adr/0007-apr-calculation-methodology.md)
- [0008: Frontend React TypeScript Vite](adr/0008-frontend-react-typescript-vite.md)

---

## 🗄️ Database Documentation
- [Database Schema](database-schema.md) — таблицы, индексы, связи, performance notes, планы партиционирования

---

## 🚀 Deployment Documentation
- [Docker Guide](deployment/docker.md) — запуск, управление, логи, отладка, мониторинг, безопасность

---

## 🎨 Frontend Components
- [Components Index](frontend/components/index.md) — обзор, CSS переменные, биржевые цвета, hooks, container
- [CurrentDataTable](frontend/components/CurrentDataTable.md) — таблица текущих ставок
- [HistoryTable](frontend/components/HistoryTable.md) — APR статистика по периодам
- [HistoryPanel](frontend/components/HistoryPanel.md) — график истории (Recharts)
- [ArbitrageTable](frontend/components/ArbitrageTable.md) — арбитражные возможности
- [CompactFilter](frontend/components/CompactFilter.md) — поиск монеты + мультиселект бирж
- [CoinSelector](frontend/components/CoinSelector.md) — выпадающий селектор монеты
- [ExchangeSelector](frontend/components/ExchangeSelector.md) — toggle-кнопки бирж

---

## 🔌 API Endpoints

| Method | Path | Описание |
|--------|------|----------|
| GET | `/api/v1/FundingRates` | Текущие ставки (`symbol?`, `exchanges?`, `includeInactive?`) |
| GET | `/api/v1/History` | История ставок (`symbol`, `exchanges?`, `from?`, `to?`, `limit?`) |
| GET | `/api/v1/History/apr-stats` | APR статистика (`symbol`, `exchanges?`) |
| GET | `/api/v1/Arbitrage` | Арбитражные возможности, отсортированные по APR (`symbol?`, `exchanges?`) |
| GET | `/api/v1/exchanges/health` | Доступность API бирж |

---

## 🔗 Quick Links

| Ресурс | Ссылка |
|--------|--------|
| **Swagger UI (Dev)** | http://localhost:5000/ |
| **Frontend (Dev)** | http://localhost:5173 |
| **GitHub Repository** | (добавить ссылку) |
| **Issues** | (добавить ссылку) |

---

## 📝 Обновление документации

Документация обновляется вместе с кодом:
- **API** — XML комментарии в DTO/Контроллерах → Swagger UI
- **ADR** — при принятии архитектурных решений
- **Components** — при изменении UI компонентов
- **Database** — при миграциях схемы
- **Deployment** — при изменении инфраструктуры