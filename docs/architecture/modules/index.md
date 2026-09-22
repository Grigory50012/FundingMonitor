# Модули FundingMonitor

Каждый бизнес-модуль имеет собственные `Contracts`, `Domain`, `Application`, `Infrastructure` и `Presentation`. Страница получает статус «Проверено» только после сверки с production-кодом и профильными тестами.

| Модуль | Ответственность | Данные | Статус документации |
| --- | --- | --- | --- |
| [ExchangeIntegration](exchange-integration.md) | Доступ к API бирж и нормализация | Без persistence | Проверено 2026-09-15 |
| [CurrentRates](current-rates.md) | Текущие ставки и их актуальность | `current_rates`, Outbox | Проверено 2026-09-22 |
| [HistoricalRates](historical-rates.md) | История, APR и RabbitMQ consumers | `historical_rates` | Проверено 2026-09-22 |
| [Arbitrage](arbitrage.md) | Расчёт возможностей между биржами | Только память | Проверено 2026-09-22 |

Страницы модулей добавляются последовательно после проверки. Общие зависимости описаны в [архитектуре](../index.md), а потоки между несколькими модулями — в [сквозных сценариях](../scenarios.md).
