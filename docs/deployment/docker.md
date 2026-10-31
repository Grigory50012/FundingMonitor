# FundingMonitor — Docker

Docker Compose поднимает инфраструктуру; backend и frontend запускаются отдельно.

## Компоненты

| Сервис | Порты | Назначение |
| --- | --- | --- |
| PostgreSQL 17 | `5432` | Current и historical funding rates |
| RabbitMQ 4.2 Management | `5672`, `15672` | Historical tasks и Management UI |

## Запуск

```bash
docker compose up -d --remove-orphans
docker compose ps
```

Оба контейнера должны перейти в состояние `healthy`.

```text
funding_monitor_db
funding_monitor_rabbitmq
```

Management UI: `http://localhost:15672`, local credentials: `guest` / `guest`.

## RabbitMQ topology

HistoricalRates идемпотентно создаёт direct exchange `current-rates.events` и durable classic queues:

```text
historical-rates.collect.binance
historical-rates.collect.bybit
historical-rates.collect.okx
historical-rates.dead
```

Проверить очереди можно в Management UI или командой:

```bash
docker exec funding_monitor_rabbitmq rabbitmqctl list_queues name consumers messages_ready messages_unacknowledged
```

При работающем ApiHost каждая collection queue имеет по умолчанию 10 consumers: три независимых worker pool с `prefetch=1` и manual ACK. `historical-rates.dead` не имеет автоматического consumer. Просмотр и replay выполняются оператором вручную после устранения причины ошибки.

CurrentRates отправляет события через PostgreSQL Outbox и publisher confirms. HistoricalRates сохраняет историю одним атомарным upsert до ACK. Inbox отсутствует; повторная доставка повторяет API-запрос, но не создаёт дубли строк. Retry/DLQ также подтверждаются брокером перед ACK исходного сообщения. После остановки ApiHost число consumers становится нулевым; ошибки внешних бирж и географические ограничения диагностируются отдельно от topology брокера.

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
\dt current_rates.*
\dt historical_rates.*
SELECT * FROM current_rates."__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
SELECT * FROM historical_rates."__EFMigrationsHistory" ORDER BY "MigrationId" DESC;
```

Схемы принадлежат разным DbContext и имеют отдельные истории миграций. Миграции применяет только `DatabaseMigrator`; ApiHost не изменяет схему при старте.

## Запуск backend и проверка

После готовности Compose из корня репозитория выполните:

```bash
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost --no-launch-profile -- --urls http://127.0.0.1:5000 --environment Development
```

ApiHost запускается только после успешного завершения migrator. Эта команда выбирает HTTP на IPv4 без HTTPS launch profile и подходит для проверки backend отдельно. Для работы с frontend запускайте `dotnet run --project src/FundingMonitor.ApiHost` со стандартным launch profile: dev-прокси Vite обращается напрямую к `https://localhost:5001`. В Development доступны Scalar `/scalar` и OpenAPI `/openapi/v1.json`. Во втором терминале проверьте реальные ответы HTTP-only запуска:

```bash
curl --noproxy "*" http://127.0.0.1:5000/openapi/v1.json
curl --noproxy "*" http://127.0.0.1:5000/api/v1/FundingRates
curl --noproxy "*" http://127.0.0.1:5000/api/v1/Arbitrage
curl --noproxy "*" http://127.0.0.1:5000/health
```

На Windows используйте `curl.exe`. При исправной системе ожидается HTTP 200; один занятый порт не доказывает работоспособность. `/health` выполняет live AMQP probe exchange и четырёх очередей с бюджетом отмены 5 секунд. Потеря брокера или topology даёт `Degraded`, восстановление — `Healthy`; HTTP 200 возможен для обоих статусов, поэтому проверяйте тело ответа. Passive probe не создаёт очереди, не проверяет bindings, consumers или внешние биржи. Число consumers проверяется указанной выше командой отдельно. Frontend запускается командой `npm run dev` из `frontend/FundingMonitor.Web`.

## Production

Перед production deployment:

- заменить credentials PostgreSQL и RabbitMQ;
- хранить secrets вне tracked configuration;
- ограничить доступ к `5432`, `5672` и `15672`;
- настроить backup PostgreSQL и RabbitMQ volume;
- не считать single-node classic queues защитой от физической потери диска.

## Обновление упрощённого Outbox

Запускается ровно один экземпляр ApiHost: Outbox dispatcher не использует распределённый захват записей. При обновлении остановить старый ApiHost, выполнить DatabaseMigrator и запустить новую версию. Миграции удаляют лишние поля Outbox и таблицу Inbox, сохраняя события и историю. Не запускать старую версию одновременно с миграцией или новой версией. См. [ADR 0013](../adr/0013-simple-outbox-without-inbox.md).

## Восстановление схем и доступ к frontend

ApiHost не применяет миграции. После удаления схем остановите backend, запустите DatabaseMigrator, затем ApiHost. Мигратор создаёт актуальные таблицы, но не восстанавливает удалённые данные.

Frontend запускается командой npm run dev из frontend/FundingMonitor.Web. Открывайте http://127.0.0.1:5173 — Vite привязан к IPv4. При отказе подключения проверьте адрес и порт в выводе Vite.
