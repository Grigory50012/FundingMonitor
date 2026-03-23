# FundingMonitor

**Система мониторинга ставок финансирования криптовалютных фьючерсов**

Приложение собирает данные о ставках финансирования с бирж Binance и Bybit, сохраняет историю в PostgreSQL и предоставляет API для получения текущих ставок и исторических данных.

---

## 📋 Оглавление

- [Возможности](#-возможности)
- [Архитектура](#-архитектура)
- [Технологии](#-технологии)
- [Быстрый старт](#-быстрый-старт)
- [API](#-api)
- [Конфигурация](#-конфигурация)
- [Логирование](#-логирование)
- [Разработка](#-разработка)
- [Статус](#-статус)

---

## ✨ Возможности

- **Сбор данных в реальном времени** — ставки финансирования каждые 10 секунд
- **Поддержка нескольких бирж** — Binance, Bybit (легко добавить новые)
- **Исторические данные** — хранение всей истории в PostgreSQL
- **REST API** — получение текущих и исторических данных
- **Автоматическое обнаружение** — новые символы и изменения времени выплаты
- **Персистентная очередь** — задачи не теряются при перезапуске (Redis)
- **Глобальная обработка ошибок** — ProblemDetails (RFC 7807)
- **Декомпозированное логирование** — 4 файла для разных типов событий
- **Docker** — PostgreSQL + Redis в один клик

---

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│  FundingMonitor.Api                                         │
│  ├── Controllers (FundingRates, History)                   │
│  ├── Middleware (ExceptionHandling)                        │
│  └── DTOs / ProblemDetails                                 │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│  FundingMonitor.Application                                 │
│  ├── Services (6)                                          │
│  └── BackgroundServices (2)                                │
│      ├── CurrentCollectionBackgroundService                │
│      └── HistoricalCollectionBackgroundService             │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│  FundingMonitor.Core                                        │
│  ├── Entities (Domain Models)                              │
│  ├── Interfaces                                            │
│  └── Events                                                │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────┴────────────────────────────────────────┐
│  FundingMonitor.Infrastructure                              │
│  ├── Data (EF Core + PostgreSQL)                           │
│  ├── ExchangeClients (Binance, Bybit)                      │
│  └── Queues (Redis)                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Технологии

### **Backend**

| Компонент       | Технология            | Версия |
| --------------- | --------------------- | ------ |
| **Framework**   | .NET                  | 10.0   |
| **API**         | ASP.NET Core          | 10.0   |
| **База данных** | PostgreSQL            | 17     |
| **ORM**         | Entity Framework Core | 10.0   |
| **Очередь**     | Redis                 | 7      |
| **Логирование** | NLog                  | 6.1    |
| **Binance API** | Binance.Net           | 12.10  |
| **Bybit API**   | Bybit.Net             | 6.9    |

### **Frontend**

| Компонент       | Технология   | Версия |
| --------------- | ------------ | ------ |
| **Framework**   | React        | 19.1   |
| **Язык**        | TypeScript   | 5.8    |
| **Сборка**      | Vite         | 7.1    |
| **Стили**       | Tailwind CSS | 4.2    |
| **HTTP клиент** | Axios        | 1.13   |
| **Графики**     | Recharts     | 3.8    |

### **Инфраструктура**

| Компонент            | Технология      | Версия |
| -------------------- | --------------- | ------ |
| **Контейнеризация**  | Docker          | latest |
| **Оркестрация**      | Docker Compose  | 3.8    |
| **Документация API** | Swagger/OpenAPI | 10.1   |

---

## 🚀 Быстрый старт

### **1. Требования**

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) (опционально)

### **2. Запуск инфраструктуры (Docker)**

```bash
# Запустить PostgreSQL + Redis
docker-compose up -d

# Проверить статус
docker-compose ps
```

**Ожидаемый результат:**

```
NAME                      STATUS
funding_monitor_db        Up (healthy)
funding_monitor_redis     Up (healthy)
```

### **3. Запуск приложения**

```bash
# Восстановить зависимости
dotnet restore

# Запустить API
dotnet run --project src/FundingMonitor.Api
```

### **4. Открыть Swagger UI**

```
http://localhost:5000
```

---

## 📡 API

### **Endpoints**

| Метод | Endpoint                    | Описание            |
| ----- | --------------------------- | ------------------- |
| `GET` | `/api/v1/FundingRates`      | Текущие ставки      |
| `GET` | `/api/v1/History`           | Исторические данные |
| `GET` | `/api/v1/History/apr-stats` | APR статистика      |

### **Примеры запросов**

#### **1. Получить все текущие ставки**

```bash
GET http://localhost:5000/api/v1/FundingRates
```

**Ответ:**

```json
[
  {
    "exchange": "Binance",
    "symbol": "BTC-USDT",
    "markPrice": 95000.5,
    "fundingRate": 0.0001,
    "apr": 10.95,
    "numberOfPaymentsPerDay": 3,
    "nextFundingTime": "2026-03-23T16:00:00Z"
  }
]
```

#### **2. Получить ставки для конкретного символа**

```bash
GET http://localhost:5000/api/v1/FundingRates?symbol=BTC
```

#### **3. Получить исторические данные**

```bash
GET http://localhost:5000/api/v1/History?symbol=BTC-USDT&limit=100
```

#### **4. Получить APR статистику**

```bash
GET http://localhost:5000/api/v1/History/apr-stats?symbol=BTC-USDT
```

**Ответ:**

```json
[
  {
    "exchange": "Binance",
    "period": "1 день",
    "days": 1,
    "apr": 10.95,
    "totalFundingRatePercent": 0.03,
    "paymentsCount": 3,
    "avgFundingRatePercent": 0.01
  }
]
```

---

## ⚙️ Конфигурация

### **appsettings.json**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=funding_monitor;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "CurrentDataCollectionOptions": {
    "UpdateIntervalSeconds": 10
  },
  "HistoricalDataCollectionOptions": {
    "MaxConcurrentTasks": 10,
    "ApiPageSize": 1000,
    "MaxHistoryMonths": 1,
    "MaxRetries": 3
  }
}
```

### **Параметры**

| Параметр                | Значение | Описание                               |
| ----------------------- | -------- | -------------------------------------- |
| `UpdateIntervalSeconds` | 10       | Интервал сбора текущих ставок          |
| `MaxConcurrentTasks`    | 10       | Макс. параллельных задач сбора истории |
| `ApiPageSize`           | 1000     | Размер страницы API бирж               |
| `MaxHistoryMonths`      | 1        | Глубина истории для новых символов     |
| `MaxRetries`            | 3        | Макс. попыток при ошибке               |

---

## 📝 Логирование

### **Файлы логов**

| Файл                         | Назначение                  |
| ---------------------------- | --------------------------- |
| `logs/{date}/startup.log`    | Миграции БД, старт хостинга |
| `logs/{date}/collection.log` | Сбор данных с бирж          |
| `logs/{date}/api.log`        | HTTP запросы к API          |
| `logs/{date}/errors.log`     | Ошибки (Error+)             |

### **Пример лога**

```
2026-03-23 19:25:18.7278|INFO|Collection cycle completed: 1113 rates, 0 events, 2195ms
2026-03-23 19:25:18.7278|INFO|Collection cycle completed: 1113 rates, 2198ms
```

---

## 👨‍💻 Разработка

### **Структура проекта**

```
FundingMonitor/
├── src/
│   ├── FundingMonitor.Api/              # Controllers, DTOs, Middleware
│   ├── FundingMonitor.Application/      # Services, BackgroundServices
│   ├── FundingMonitor.Core/             # Entities, Interfaces, Events
│   └── FundingMonitor.Infrastructure/   # EF Core, ExchangeClients, Redis
├── frontend/
│   └── FundingMonitor.Web/              # React + TypeScript + Vite
├── docker-compose.yml                   # PostgreSQL + Redis
├── README.Docker.md                     # Docker документация
└── README.md                            # Этот файл
```

### **Сборка**

```bash
# Сборка решения
dotnet build

# Запуск тестов (если есть)
dotnet test

# Публикация
dotnet publish -c Release
```

### **Запуск фронтенда**

```bash
# Перейти в папку фронтенда
cd frontend/FundingMonitor.Web

# Установить зависимости
npm install

# Запустить dev-сервер
npm run dev

# Сборка production версии
npm run build
```

### **Миграции БД**

```bash
# Добавить миграцию
dotnet ef migrations add MigrationName --project src/FundingMonitor.Infrastructure

# Применить миграции
dotnet ef database update --project src/FundingMonitor.Infrastructure
```

---

## 📊 Статус

| Компонент            | Статус                |
| -------------------- | --------------------- |
| **Сбор данных**      | ✅ Binance, Bybit     |
| **Хранение**         | ✅ PostgreSQL         |
| **API**              | ✅ REST + Swagger     |
| **Очередь**          | ✅ Redis              |
| **Логирование**      | ✅ NLog (4 файла)     |
| **Обработка ошибок** | ✅ ProblemDetails     |
| **Docker**           | ✅ PostgreSQL + Redis |
| **Frontend**         | ✅ React + TypeScript |
| **Тесты**            | ⏳ В планах           |

---

## 📝 Лицензия

MIT

---

## 📞 Контакты

Вопросы и предложения: [Your Email]
