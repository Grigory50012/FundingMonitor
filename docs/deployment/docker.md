# FundingMonitor — Docker

Docker Compose поднимает инфраструктуру; backend и frontend запускаются отдельно.

## Компоненты

| Сервис | Порты | Назначение |
| --- | --- | --- |
| PostgreSQL 17 | `5432` | Current и historical funding rates |
| RabbitMQ 4.2 Management | `5672`, `15672` | Historical tasks и Management UI |

## Запуск

```bash
docker compose up -d
docker compose ps
```

Оба контейнера должны перейти в состояние `healthy`.

```text
funding_monitor_db
funding_monitor_rabbitmq
```

Management UI: `http://localhost:15672`, local credentials: `guest` / `guest`.

## RabbitMQ topology

Приложение идемпотентно создаёт direct exchange `funding.history` и durable classic queues:

```text
history.binance
history.bybit
history.okx
history.dead
```

Проверить очереди можно в Management UI или командой:

```bash
docker exec funding_monitor_rabbitmq rabbitmqctl list_queues name messages_ready messages_unacknowledged consumers
```

`history.dead` не имеет автоматического consumer. Просмотр и replay выполняются оператором вручную после устранения причины ошибки.

## Consumer acknowledgement timeout

Файл `infrastructure/rabbitmq/rabbitmq.conf` монтируется read-only и содержит:

```ini
consumer_timeout = 1800000
```

Тридцать минут превышают известное пятиминутное ожидание Binance SDK limiter. Это broker-side setting и не дублируется в `appsettings.json`.

Проверка effective configuration:

```bash
docker exec funding_monitor_rabbitmq rabbitmq-diagnostics environment
```

## Управление и логи

```bash
docker compose logs -f
docker compose logs -f postgres
docker compose logs -f rabbitmq
docker compose restart rabbitmq
docker compose down
```

`docker compose down` сохраняет данные. Команда `docker compose down -v` удаляет volumes `postgres_data` и `rabbitmq_data`; использовать её только когда данные действительно больше не нужны.

## PostgreSQL

```bash
docker exec -it funding_monitor_db psql -U postgres -d funding_monitor
```

```sql
\dt
SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
```

Migrations применяются backend через `Database.MigrateAsync()` при запуске.

## Production

Перед production deployment:

- заменить credentials PostgreSQL и RabbitMQ;
- хранить secrets вне tracked configuration;
- ограничить доступ к `5432`, `5672` и `15672`;
- настроить backup PostgreSQL и RabbitMQ volume;
- не считать single-node classic queues защитой от физической потери диска.
