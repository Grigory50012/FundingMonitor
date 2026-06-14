# Contributing to FundingMonitor

Спасибо за интерес к проекту! Приветствуем любые вклады: баг-репорты, фичи, улучшения docs, рефакторинг.

---

## Quick Start

```bash
# 1. Fork & Clone
git clone https://github.com/YOUR_FORK/FundingMonitor.git
cd FundingMonitor

# 2. Infrastructure
docker-compose up -d

# 3. Backend
dotnet restore
dotnet run --project src/FundingMonitor.Api

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
- **API типы**: пока вручную в `types/`, планируется codegen из OpenAPI

---

## Architecture Guidelines

### Новые биржи (Exchange)

1. Создайте `XxxFundingRateClient` в `Infrastructure/ExchangeClients`
2. Реализуйте `IExchangeFundingRateClient` из `Core`
3. Зарегистрируйте в `Infrastructure/Extensions/ServiceCollectionExtensions.cs`
4. Добавьте `ExchangeType` в `Core/Entities/ExchangeType.cs`
5. Обновите `ExchangeTypeExtensions.ParseSingle` (или используйте кэш)

### Новые Background Services

1. Наследуйтесь от `BackgroundService`
2. Инжектируйте `ILogger<T>`, сервисы через конструктор
3. Уважайте `CancellationToken` — проверяйте в циклах
4. Регистрируйте в `Application/Extensions/ServiceCollectionExtensions.cs`

### Database Changes

1. Измените Entity в `Infrastructure/Data/Entities/`
2. Добавьте конфигурацию в `FundingMonitorDbContext.OnModelCreating`
3. Создайте миграцию:
   ```bash
   dotnet ef migrations add MigrationName --project src/FundingMonitor.Infrastructure
   ```
4. Проверьте SQL (`dotnet ef migrations script`)
5. **Не редактируйте** уже применённые миграции

---

## Testing

> ⚠️ Тесты пока в планах. При добавлении функциональности — пишите тесты.

Планируемый стек:
- **Unit**: xUnit + Moq + FluentAssertions
- **Integration**: Testcontainers (PostgreSQL, Redis)
- **API**: `WebApplicationFactory<Program>`
- **Frontend**: Vitest + React Testing Library + Playwright (E2E)

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