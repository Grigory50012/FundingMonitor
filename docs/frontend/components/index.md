# Frontend Components

Документация по текущей frontend-структуре `frontend/FundingMonitor.Web`.

## Текущая структура

| Слой | Путь | Назначение |
| --- | --- | --- |
| App | `src/app/providers` | Application-level provider boundary |
| Widgets | `src/widgets/dashboard` | Dashboard page и orchestration hook |
| Features | `src/features/current-rates` | Таблица текущих funding rates |
| Features | `src/features/historical-rates` | History API, график и APR table |
| Features | `src/features/arbitrage` | Funding arbitrage table |
| Features | `src/features/exchange-health` | Exchange health API и state |
| Features | `src/features/filters` | Активный фильтр символа и бирж |
| Entities | `src/entities/exchange` | Exchange constants и badge |
| Shared UI | `src/shared/ui` | `Panel`, `Spinner`, `EmptyState`, `SortIcon` |
| Shared API | `src/shared/api` | Axios client и API error helper |
| Shared lib/hooks | `src/shared/lib`, `src/shared/hooks` | Formatting, sorting, symbols и local storage |

## Активные UI части

| Документ | Реальная реализация | Назначение |
| --- | --- | --- |
| [CurrentDataTable](CurrentDataTable.md) | `src/features/current-rates/CurrentRatesTable.tsx` | Текущие rates, сортировка, exchange links |
| [HistoryTable](HistoryTable.md) | `src/features/historical-rates/HistoryAprTable.tsx` | APR statistics по периодам |
| [HistoryPanel](HistoryPanel.md) | `src/features/historical-rates/HistoryChartPanel.tsx` | График исторических rates |
| [ArbitrageTable](ArbitrageTable.md) | `src/features/arbitrage/ArbitrageTable.tsx` | Funding arbitrage opportunities |
| [CompactFilter](CompactFilter.md) | `src/features/filters/CompactFilter.tsx` | Поиск символа и мультивыбор бирж |

## Удалённые legacy-компоненты

`CoinSelector` и `ExchangeSelector` удалены; их функции объединены в `CompactFilter`. Legacy `src/components`, `src/containers`, root `src/api` и feature-agnostic data hooks также удалены: widgets импортируют feature owners напрямую.

## Data Flow

```text
DashboardPage
-> useDashboardData
-> useCurrentRates / useHistoryRates / useArbitrageRates
-> feature-owned endpoint functions
-> /api/v1/*
```

APR table дополнительно использует `useAprStats`. API-функции каждого feature используют общий Axios client и generated DTO types; feature internals не импортируют друг друга. Их composition разрешён только в `widgets` и `app`.

## State

- Фильтры сохраняются через `useLocalStorage`.
- Основной фильтр и arbitrage-фильтр хранятся отдельно.
- История имеет режимы `chart` и `table`.
- TanStack Query, Redux и Zustand сейчас не используются.

## Связанные заметки

- [[../../architecture/index|Архитектура]]
- [[../../project/overview|Описание проекта]]
