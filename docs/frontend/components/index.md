# Frontend Components

Документация по текущей frontend-структуре `frontend/FundingMonitor.Web`.

## Текущая структура

Общая типографика панелей задаётся в `frontend/FundingMonitor.Web/src/index.css`: переменные `--panel-title-size` (16 px), `--panel-text-size` (12 px), `--panel-caption-size` (10 px). Классы `panel-title`, `panel-text`, `panel-value` и `panel-caption` применяются к заголовкам, основному тексту, числовым значениям и вторичным подписям. `Panel` задаёт общий основной размер через `dashboard-panel`; `dashboard-control` использует тот же размер текста. Для изменения масштаба редактируйте эти переменные, а не отдельные компоненты.

| Слой | Путь | Назначение |
| --- | --- | --- |
| App | `src/app/providers` | Application-level provider boundary |
| Widgets | `src/widgets/dashboard` | Dashboard page и orchestration hook |
| Features | `src/features/current-rates` | Таблица текущих funding rates |
| Features | `src/features/historical-rates` | History API, график и APR table |
| Features | `src/features/arbitrage` | Funding arbitrage table |
| Features | `src/features/filters` | Активный фильтр символа и бирж |
| Entities | `src/entities/exchange` | Exchange constants и badge |
| Shared UI | `src/shared/ui` | `Panel`, `Spinner`, `EmptyState`, `SortIcon` |
| Shared API | `src/shared/api` | Axios client и API error helper |
| Shared lib/hooks | `src/shared/lib`, `src/shared/hooks` | Formatting, sorting, symbols и local storage |

## Активные UI части

`DashboardPage` объединяет текущие ставки и историю в общую панель «Ставки финансирования». В её шапке находятся общие поиск монеты и выбор бирж. На широком экране части занимают 40% и 60% ширины и разделены вертикальной линией; на узком расположены друг под другом с горизонтальным разделителем. Периоды и переключатель графика/APR остаются в заголовке части «История».

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

`useDashboardData` дополнительно вызывает `useAprStats` в режиме APR-таблицы и задаёт общий порядок бирж и сортировку для текущих ставок и истории. API-функции каждого feature используют общий Axios client и generated DTO types; feature internals не импортируют друг друга. Их composition разрешён только в `widgets` и `app`.

Backend состоит из модулей `CurrentRates`, `HistoricalRates`, `Arbitrage` и `ExchangeIntegration`, зарегистрированных в `FundingMonitor.ApiHost`. HTTP-маршруты и OpenAPI-контракт сохранены. APR, spread и freshness рассчитываются на backend; frontend отвечает за отображение, фильтры, сортировку и состояние UI. Типы обновляются через `npm run generate:api-types` из единого `/openapi/v1.json`.

## State

- Фильтры сохраняются через `useLocalStorage` и восстанавливаются после перезагрузки страницы.
- Основной фильтр и arbitrage-фильтр хранятся отдельно.
- История имеет режимы `chart` и `table`; выбранный режим и период графика также сохраняются в `localStorage`.
- Общая сортировка текущих ставок и APR-истории, сортировка арбитража, размер и номер страницы арбитража сохраняются после перезагрузки. При изменении фильтров, сортировки или размера страницы арбитраж возвращается на первую страницу; при уменьшении результатов выбирается последняя доступная страница после загрузки данных.
- TanStack Query управляет серверными данными; Redux и Zustand сейчас не используются.

## Связанные заметки

- [Архитектура](../../architecture/index.md)
- [Описание проекта](../../project/overview.md)
