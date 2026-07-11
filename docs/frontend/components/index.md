# Frontend Components

Документация по текущей frontend-структуре `frontend/FundingMonitor.Web`.

## Текущая структура

| Слой | Путь | Назначение |
| --- | --- | --- |
| Widgets | `src/widgets/dashboard` | Dashboard page и orchestration hook |
| Features | `src/features/current-rates` | Таблица текущих funding rates |
| Features | `src/features/history` | График истории и APR table |
| Features | `src/features/arbitrage` | Funding arbitrage table |
| Features | `src/features/filters` | Активный фильтр символа и бирж |
| Entities | `src/entities/exchange` | Exchange constants и badge |
| Shared UI | `src/shared/ui` | `Panel`, `Spinner`, `EmptyState`, `SortIcon` |
| Shared lib | `src/shared/lib` | Formatting, sorting, symbol helpers |
| Compatibility | `src/components` | Barrel exports для старых импортов |

## Активные UI части

| Документ | Реальная реализация | Назначение |
| --- | --- | --- |
| [CurrentDataTable](CurrentDataTable.md) | `src/features/current-rates/CurrentRatesTable.tsx` | Текущие rates, сортировка, exchange links |
| [HistoryTable](HistoryTable.md) | `src/features/history/HistoryAprTable.tsx` | APR statistics по периодам |
| [HistoryPanel](HistoryPanel.md) | `src/features/history/HistoryChartPanel.tsx` | График исторических rates |
| [ArbitrageTable](ArbitrageTable.md) | `src/features/arbitrage/ArbitrageTable.tsx` | Funding arbitrage opportunities |
| [CompactFilter](CompactFilter.md) | `src/features/filters/CompactFilter.tsx` | Поиск символа и мультивыбор бирж |

## Удалённые legacy-компоненты

`CoinSelector` и `ExchangeSelector` удалены из `src/components`. Их функции объединены в `CompactFilter`.

## Data Flow

```text
DashboardPage
-> useDashboardData
-> useCurrentRates / useHistoryRates / useArbitrageRates
-> fundingRatesApi
-> /api/v1/*
```

APR table дополнительно использует `useAprStats`.

## State

- Фильтры сохраняются через `useLocalStorage`.
- Основной фильтр и arbitrage-фильтр хранятся отдельно.
- История имеет режимы `chart` и `table`.
- TanStack Query, Redux и Zustand сейчас не используются.

## Связанные заметки

- [[../../dashboard|Дашборд]]
- [[../../architecture/index|Архитектура]]
- [[../../project/status|Статус проекта]]
