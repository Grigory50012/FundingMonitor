# HistoryTable

Таблица APR статистики по периодам (1д, 2д, 3д, 7д, 14д, 21д, 30д) для выбранных бирж и символа.

## Путь
`frontend/FundingMonitor.Web/src/components/HistoryTable.tsx`

## Props

```typescript
interface HistoryTableProps {
  selectedExchanges: ExchangeType[]; // Фильтр по биржам
  symbol: string;                    // Символ (например, "BTC-USDT")
}
```

## Функциональность

### Загрузка данных
- Самостоятельно загружает данные через `fundingRatesApi.getAprStats()`
- `useEffect` с зависимостями `[symbol, selectedExchanges]`
- Состояния: `isLoading`, `error`, `data`

### Сортировка
- Сортировка **бирж** (строк) внутри выбранного периода
- Клик по заголовку периода устанавливает `sortConfig.period` и колонку
- Колонки для сортировки: **APR**, **∑ (суммарная ставка)**, **Выплат**, **Средняя ставка**
- Цикл: `asc` → `desc` → `none`

### Отображаемые метрики (на ячейку периода)
| Метрика | Формат | Описание |
|---------|--------|----------|
| **APR** | `XX.XX%` | Годовая процентная ставка, цвет по знаку |
| **∑** | `∑ X.XXX%` | Суммарная ставка за период |
| **Count** | `N` | Количество выплат |
| **Avg** | `X.XXX%` | Средняя ставка за выплату |
| **σ** | `σ X.XXXX%` | Стандартное отклонение (фиолетовый) |

### UI детали
- Sticky заголовок + sticky первый столбец (Биржа)
- Колонки периодов генерируются из `PERIODS` константы
- Цветные бейджи бирж (как в CurrentDataTable)
- Под таблицей — легенда с расшифровкой метрик
- Пустые состояния: загрузка, ошибка, нет данных

## Константы

```typescript
// Из ../types
const PERIODS = [
  { label: "1 день", days: 1 },
  { label: "2 дня", days: 2 },
  { label: "3 дня", days: 3 },
  { label: "7 дней", days: 7 },
  { label: "14 дней", days: 14 },
  { label: "21 день", days: 21 },
  { label: "30 дней", days: 30 },
] as const;
```

> Периоды на бэкенде задаются в `appsettings.json` → `AprStats:Periods` (по умолчанию `[1, 2, 3, 7, 14, 21, 30]`). Query-параметр `periods` в API пока не поддерживается.

## Зависимости
- `AprPeriodStatsDto`, `ExchangeType`, `PERIODS` из `../types`
- `fundingRatesApi` из `../api/fundingRates`

## Пример использования

```tsx
<HistoryTable
  symbol="BTC-USDT"
  selectedExchanges={['Binance', 'Bybit']}
/>
```

## Особенности
- Компонент **сам загружает данные** (smart component), а не получает готовые пропсы
- Двойная фильтрация: на бэкенде (через API params) + на фронте (fallback)
- Сортировка работает только по видимым биржам