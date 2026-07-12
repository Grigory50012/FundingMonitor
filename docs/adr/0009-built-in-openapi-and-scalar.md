# ADR 0009: встроенный OpenAPI и Scalar

**Статус**: Принято

**Дата**: 2026-07-12

## Контекст

Frontend DTO должны синхронизироваться с backend contract без ручного дублирования. Первоначальный вариант использовал Swashbuckle и отдельный exporter OpenAPI document, но для текущего масштаба проекта exporter, сохранённая schema и drift-check создавали лишнюю инфраструктуру.

## Решение

- Использовать встроенный `Microsoft.AspNetCore.OpenApi 10.0.9` через `AddOpenApi()` и `MapOpenApi()`.
- Публиковать runtime document по адресу `/openapi/v1.json` только в Development environment.
- Использовать `Scalar.AspNetCore 2.16.11` для интерактивной документации по адресу `/scalar`.
- Генерировать только TypeScript types командой `npm run generate:api-types`; Axios client остаётся ручным.
- Использовать `JsonNumberHandling.Strict`, чтобы числовые DTO имели однозначный JSON/OpenAPI contract.
- Закрепить `Microsoft.OpenApi 2.10.0` как совместимую security-pinned транзитивную версию. Ветка `3.x` несовместима с source generator из текущего `Microsoft.AspNetCore.OpenApi`.

## Последствия

Плюсы:

- Нет Swashbuckle, собственного exporter и сохранённой OpenAPI schema.
- OpenAPI generation использует официальный ASP.NET Core stack.
- Scalar даёт интерактивный UI без влияния на API contract.
- Backend DTO остаются источником generated frontend types.

Минусы:

- Для обновления TypeScript types необходимо запустить backend и локальную инфраструктуру.
- Автономной CI-проверки contract drift пока нет.
- Generated file необходимо обновлять вручную после изменения DTO.

## Альтернативы

| Вариант | Причина отказа |
| --- | --- |
| Ручные frontend DTO | Уже привели к расхождению контракта, включая отсутствующий `aprSpread` |
| Swashbuckle + runtime Swagger | Работает, но дублирует встроенные возможности ASP.NET Core |
| Собственный exporter + committed schema | Надёжнее для CI, но избыточен для текущего solo-dev workflow |
| Полная генерация Axios client | Не нужна: текущий ручной API layer мал и понятен |

## Текущие ключевые версии

| Package | Version |
| --- | --- |
| `Microsoft.AspNetCore.OpenApi` | `10.0.9` |
| `Microsoft.OpenApi` | `2.10.0` |
| `Microsoft.EntityFrameworkCore` | `10.0.9` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` |
| `Scalar.AspNetCore` | `2.16.11` |
| `NLog` / `NLog.Web.AspNetCore` | `6.1.4` |
| `Binance.Net` | `13.1.0` |
| `Bybit.Net` | `7.1.0` |
| `JK.OKX.Net` | `5.1.0` |
| `StackExchange.Redis` | `3.0.17` |

Project files остаются источником истины для актуальных package versions; таблица фиксирует состояние на момент принятия решения.
