# Contributing to FundingMonitor

Спасибо за интерес к проекту! Приветствуем любые вклады: баг-репорты, фичи, улучшения docs, рефакторинг.

---

## Quick Start

```bash
# 1. Fork & Clone
git clone https://github.com/YOUR_FORK/FundingMonitor.git
cd FundingMonitor

# 2. Infrastructure
docker compose up -d --remove-orphans

# 3. Backend
dotnet restore
dotnet run --project src/FundingMonitor.DatabaseMigrator
dotnet run --project src/FundingMonitor.ApiHost

# 4. Frontend
cd frontend/FundingMonitor.Web
npm install
npm run dev
```

---

## Development Workflow

### 1. Ветвление

```
main
  └── feature/short-description     # Новые фичи
  └── fix/short-description         # Багфиксы
  └── docs/short-description        # Документация
  └── refactor/short-description    # Рефакторинг
  └── chore/short-description       # Зависимости, конфиг, CI
```

Имена веток: `kebab-case`, префикс обязателен.

### 2. Коммиты

**Conventional Commits** (в формате `type(scope): description`):

| Type | Когда использовать |
|------|-------------------|
| `feat` | Новая функциональность |
| `fix` | Исправление бага |
| `docs` | Только документация |
| `refactor` | Рефакторинг без изменения поведения |
| `perf` | Оптимизация производительности |
| `test` | Добавление/изменение тестов |
| `chore` | Зависимости, конфиг, tooling |
| `ci` | CI/CD пайплайны |

Примеры:
```
feat(api): add arbitrage opportunities endpoint
fix(collector): handle null funding rate from Bybit
docs(adr): add ADR 0009 for rate limiting
refactor(core): extract exchange parser to separate class
```

### 3. Pull Request

1. Создайте PR в `main`
2. Заполните описание PR (шаблон `.github/pull_request_template.md` — опционально)
3. Убедитесь, что проходят проверки:
   - `dotnet build` — сборка
   - `dotnet test` — тесты (если есть)
   - `npm run lint` — frontend линтинг
   - `npm run build` — frontend сборка
4. Минимум 1 approval от maintainer
5. Squash & merge

---

## Code Style

### Backend (C#)

- **EditorConfig** — настроен в репозитории (`.editorconfig`), настройки применяются автоматически в Rider/VS
- **Nullable** — `enable` везде, избегайте `!` и `?` без нужды
- **Implicit usings** — включены
- **Records** — предпочтительнее классов для DTO/Entities
- **Primary constructors** — используйте где уместно
- **Pattern matching** — `is`, `switch` expressions
- **Async suffix** — всегда `Async` для асинхронных методов
- **CancellationToken** — передавайте во все async методы

**Форматирование** (dotnet format):
```bash
dotnet format --verify-no-changes  # Проверка
dotnet format                      # Автоисправление
```

### Frontend (TypeScript/React)

- **ESLint** — настроен в `frontend/FundingMonitor.Web`
- **Скрипты**:
  ```bash
  npm run lint      # Проверка
  npm run build     # Сборка + проверка типов
  ```
- **TypeScript**: `strict: true`, избегайте `any`
- **Компоненты**: Functional + Hooks, именуйте `PascalCase`
- **Хуки**: `use` prefix, выносите в `hooks/`
- **API типы**: `npm run generate:api-types` обновляет `src/types/generated/api.ts` из работающего `/openapi/v1.json`; Axios client остаётся ручным

---

## Architecture Guidelines

### Новые биржи (Exchange)

1. Добавьте адаптер в `src/Modules/ExchangeIntegration/FundingMonitor.ExchangeIntegration.Infrastructure`.
2. Используйте публичные интерфейсы `ExchangeIntegration.Contracts`; SDK-типы не должны выходить за Infrastructure.
3. Зарегистрируйте адаптер в DI модуля и обновите поддерживаемые биржи и конфигурацию worker pools.
4. Проверьте нормализацию, health и модульные тесты. Другие модули используют только Contracts.

### Новые Background Services

1. Наследуйтесь от `BackgroundService`
2. Инжектируйте `ILogger<T>`, сервисы через конструктор
3. Уважайте `CancellationToken` — проверяйте в циклах
4. Размещайте сервис в Infrastructure владельца и регистрируйте через DI этого модуля

### Database Changes

1. Измените persistence-модель и DbContext владельца: CurrentRates или HistoricalRates.
2. Создайте миграцию в `Infrastructure/Persistence/Migrations` этого модуля, указав его Infrastructure project и нужный `--context`; startup project — `src/FundingMonitor.DatabaseMigrator`.
3. Проверьте SQL, snapshot и integration tests; коммитьте миграцию вместе с моделью.
4. Примените `dotnet run --project src/FundingMonitor.DatabaseMigrator` до запуска ApiHost.
5. Не редактируйте уже применённые миграции и не добавляйте cross-schema queries/FK. Arbitrage не имеет persistence.

Границы четырёх модулей и пяти слоёв описаны в [архитектуре](docs/architecture/index.md) и [ADR 0012](docs/adr/0012-strict-modular-monolith.md).

---

## Testing

Backend использует отдельные модульные unit/integration suites, architecture tests и `ApiHost.IntegrationTests`. Полный прогон: `dotnet test FundingMonitor.sln`; для PostgreSQL/RabbitMQ Testcontainers нужен работающий Docker. HTTP-тесты используют `WebApplicationFactory<Program>`, подменяя внешние биржи. Во frontend обязательны `npm run lint` и `npm run build`. Добавляйте релевантные тесты вместе с изменением поведения.

---

## Reporting Issues

### Bug Report

Шаблон:
```
**Описание**: Чёткое описание бага
**Шаги воспроизведения**:
1. ...
2. ...
**Ожидаемое поведение**: ...
**Фактическое поведение**: ...
**Логи/скриншоты**: ...
**Окружение**: OS, .NET version, Node version, Docker version
```

### Feature Request

```
**Проблема**: Какую проблему решает?
**Предлагаемое решение**: Как видите реализацию?
**Альтернативы**: Что ещё рассматривали?
**Дополнительно**: Скриншоты, макеты, ссылки
```

---

## Security

Уязвимости сообщайте **приватно** на email (указать в SECURITY.md) — не создавайте публичный Issue.

---

## Code of Conduct

- Будьте уважительны и конструктивны
- Недопустимо: оскорбления, harcèlement, дискриминация
- Фокус на коде и идеях, не на личности

---

## Contacts

- Issues: GitHub Issues
- Discussions: GitHub Discussions (если включено)
- Email: (указать)

---

## License

Внося вклад, вы соглашаетесь, что ваш код распространяется под лицензией **MIT** (как весь проект).
