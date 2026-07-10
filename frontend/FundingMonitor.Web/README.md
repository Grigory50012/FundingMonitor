## Current Refactor Structure

- `src/shared/lib` contains formatting, sorting, and symbol helpers.
- `src/shared/ui` contains small reusable UI primitives.
- `src/entities/exchange` contains exchange constants and badges.
- `src/features/*` contains feature-owned dashboard components.
- `src/widgets/dashboard` contains the dashboard page and orchestration hook.
- No runtime state or data-fetching library is used; data still flows through the existing custom hooks.
- Legacy `CoinSelector` and `ExchangeSelector` components were removed; `CompactFilter` is the active selector.
# FundingMonitor.Web — Frontend

React 19 + TypeScript + Vite 7 + Tailwind CSS 4 дашборд для мониторинга funding rates.

## 🚀 Быстрый старт

```bash
cd frontend/FundingMonitor.Web
npm install
npm run dev        # Dev server с HMR на http://localhost:5173
npm run build      # Production сборка в dist/
npm run preview    # Предпросмотр production сборки
npm run lint       # ESLint проверка
```

## 📁 Структура

```
src/
├── api/                 # Axios instance + endpoint функции
├── components/          # Presentational компоненты (7 шт.)
│   ├── CurrentDataTable.tsx
│   ├── HistoryTable.tsx
│   ├── HistoryPanel.tsx
│   ├── ArbitrageTable.tsx
│   ├── CompactFilter.tsx      # используется в UI
│   ├── CoinSelector.tsx       # legacy, не используется
│   └── ExchangeSelector.tsx   # legacy, не используется
├── containers/          # Smart компоненты
│   └── DashboardContainer.tsx
├── hooks/               # Custom hooks
│   ├── useFundingRates.ts   # useCurrentRates, useHistoryRates, useArbitrageRates
│   └── useLocalStorage.ts
├── types/               # TypeScript интерфейсы (DTO)
│   ├── index.ts
│   └── history.ts
├── config/              # Константы
│   └── storageKeys.ts
├── App.tsx              # Root (рендерит DashboardContainer)
└── main.tsx             # Entry point
```

## 🎨 Компоненты

Документация: [docs/frontend/components/](../../docs/frontend/components/)

| Компонент | Назначение |
|-----------|------------|
| **DashboardContainer** | Главный smart-компонент: фильтры, загрузка данных, панели |
| **CurrentDataTable** | Таблица текущих ставок с сортировкой |
| **HistoryTable** | APR статистика по периодам (1д–30д) |
| **HistoryPanel** | График истории (Recharts) + таймрайнджы |
| **ArbitrageTable** | Арбитражные возможности с группировкой |
| **CompactFilter** | Поиск монеты + мультиселект бирж |
| **CoinSelector** | *(legacy)* Выпадающий селектор монеты |
| **ExchangeSelector** | *(legacy)* Toggle-кнопки выбора бирж |

## ⚙️ Конфигурация

### API Proxy

Базовый URL API захардкожен как `/api/v1`. В dev-режиме Vite проксирует запросы на `http://localhost:5000` (см. `vite.config.ts`).

### CSS Variables (темизация)

В `index.css` определены:
```css
--tg-bg, --tg-bg-secondary, --tg-bg-tertiary
--tg-border, --tg-text, --tg-text-secondary, --tg-text-tertiary
--tg-positive, --tg-negative, --tg-button, --tg-button-text
--tg-hint, --tg-link
```

Биржевые цвета (из `src/types/index.ts`):
- Binance: `#f0760b`
- Bybit: `#ffcc00`
- OKX: `#FFFFFF`

## 🔌 API Integration

`src/api/fundingRates.ts` — Axios instance + функции:
- `getCurrentRates(params)` → `FundingRateDto[]`
- `getHistory(params)` → `HistoricalFundingRateDto[]`
- `getAprStats(params)` → `AprPeriodStatsDto[]`
- `getArbitrageOpportunities(params)` → `FundingArbitrageDto[]` (отсортировано по APR на бэкенде)

Типы DTO в `src/types/index.ts` (ручная синхронизация с бэкендом, планируется codegen из OpenAPI).

## 🪝 Хуки

| Хук | Назначение |
|-----|------------|
| `useCurrentRates` | Загрузка текущих ставок при смене фильтров |
| `useHistoryRates` | Загрузка истории при смене фильтров |
| `useArbitrageRates` | Загрузка арбитражных возможностей |
| `useLocalStorage` | Синхронизация состояния с localStorage |

Данные обновляются при изменении параметров или по кнопке «Обновить» — автоматический поллинг пока не реализован.

## 📦 Зависимости

### Production
- `react`, `react-dom` — 19.1
- `axios` — 1.13
- `recharts` — 3.8
- `tailwindcss` — 4.2

### Dev
- `vite` — 7.1
- `typescript` — 5.8
- `eslint`, `typescript-eslint`
- `@vitejs/plugin-react`

## 🏗️ Архитектура состояния

**Нет Redux/Zustand/TanStack Query** — простая модель:
- Каждый хук держит своё состояние (`data`, `isLoading`, `error`, `refresh`)
- `useEffect` загружает данные при изменении параметров
- Фильтры в `localStorage` (persisted через `useLocalStorage`)

Планируется миграция на **TanStack Query** при росте сложности.

## 🧪 Тестирование (планируется)

- Unit: Vitest + React Testing Library
- E2E: Playwright
- Storybook: каталог компонентов

## 📝 Полезные команды

```bash
npm run dev          # Разработка
npm run build        # Сборка для production
npm run lint         # Проверка кода

# Type checking
npx tsc --noEmit     # Проверка типов без сборки
```
