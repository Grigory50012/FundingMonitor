# ADR 0010: RabbitMQ для параллельного исторического сбора

**Статус**: Принято  
**Дата**: 2026-07-26  
**Заменяет**: [ADR 0003](0003-redis-queue-for-historical-collection.md)

## Контекст

Одна Redis List и общий `SemaphoreSlim` не обеспечивали независимый прогресс бирж и удаляли задачу до успешной обработки. Binance может остановить свои запросы встроенным limiter на несколько минут; это не должно блокировать Bybit и OKX.

## Решение

Использовать `RabbitMQ.Client` напрямую и durable direct exchange `funding.history`:

- `history.binance` с routing key `binance`;
- `history.bybit` с routing key `bybit`;
- `history.okx` с routing key `okx`;
- общая `history.dead` с routing key `dead`.

Сообщения persistent и публикуются с `mandatory = true` и publisher confirms. Каждый exchange получает отдельный configurable worker pool; каждый worker имеет собственный channel, manual acknowledgement и `prefetch = 1`.

При transient error worker оставляет original message unacked, ждёт `RetryDelay`, публикует confirmed-копию в ту же exchange queue с `RetryCount + 1` и затем подтверждает original. Permanent error или исчерпание `MaxRetries` направляет confirmed-копию в `history.dead`.

В Phase 1 producer сохраняет current snapshot, затем напрямую публикует задачи. Consumer side имеет гарантию `at-least-once`; producer crash-window между PostgreSQL commit и RabbitMQ confirm пока имеет гарантию `best effort`. PostgreSQL Transactional Outbox будет отдельной итерацией и не изменит message contract, topology или consumers.

## Последствия

Положительные:

- Binance limiter не блокирует другие биржи;
- broker возвращает unacked message после падения consumer;
- retry и DLQ сохраняют неисправимые задачи;
- одинаковые события создают независимые задачи;
- Redis полностью удаляется.

Ограничения:

- до появления Outbox процесс может упасть после DB commit и до RabbitMQ confirm;
- используется один RabbitMQ node с classic queues;
- автоматический replay `history.dead` отсутствует;
- горизонтальное масштабирование backend не входит в текущую версию.

## Альтернативы

| Вариант | Причина отказа |
| --- | --- |
| Redis List | Нет manual ack, DLQ и независимого per-exchange backpressure |
| MassTransit | Лишняя абстракция для небольшой фиксированной topology |
| RabbitMQ retry queues и DLX | Сложнее необходимого для первой версии |
| PostgreSQL Outbox сразу | Перенесён во вторую итерацию для ускорения поставки parallel consumers |
