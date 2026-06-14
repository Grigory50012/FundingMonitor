# ADR 0001: Clean Architecture — разделение на слои Core, Application, Infrastructure, Api

**Статус**: Принято  
**Дата**: 2026-06-14

## Контекст

Проект требует разделения ответственностей между:
- Доменной логикой (entities, interfaces, events)
- Use cases / бизнес-логикой (services, background services)
- Инфраструктурой (DB, внешние API, queues)
- Presentation layer (API controllers, DTOs)

Нужно обеспечить тестируемость, заменяемость компонентов и независимость домена от фреймворков.

## Решение

Применяем **Clean Architecture** с 4 проектами:

```
src/
├── FundingMonitor.Core          # Domain layer (нет внешних зависимостей)
│   ├── Entities
│   ├── Interfaces (Repositories, Services)
│   └── Events
├── FundingMonitor.Application   # Use Cases / Business Logic
│   ├── Services
│   └── BackgroundServices
├── FundingMonitor.Infrastructure # External concerns
│   ├── Data (EF Core, Repositories)
│   ├── ExchangeClients (Binance, Bybit, OKX)
│   └── Queues (Redis)
└── FundingMonitor.Api           # Presentation
    ├── Controllers
    ├── DTOs
    └── Middleware
```

**Правила зависимостей**:
- `Core` — не зависит ни от чего
- `Application` → зависит от `Core`
- `Infrastructure` → зависит от `Core` (реализует интерфейсы)
- `Api` → зависит от `Application`, `Infrastructure`, `Core`

DI настраивается в `Api` через extension methods каждого слоя.

## Последствия

✅ **Плюсы**:
- Домен полностью изолирован, легко тестировать
- Можно заменить БД/очередь/биржи без изменения бизнес-логики
- Чёткое разделение ответственностей
- Подходит для масштабирования команды

❌ **Минусы**:
- Больше boilerplate (интерфейсы, маппинг)
- Начальная сложность для новичков

## Альтернативы

| Вариант | Причина отказа |
|---------|----------------|
| Monolith / Layered (Controller → Service → Repository) | Сильная связанность, сложно тестировать домен |
| Modular Monolith | Преждевременная сложность для текущего размера |
| Vertical Slice Architecture | Не подходит для shared domain (общие сущности ставок) |