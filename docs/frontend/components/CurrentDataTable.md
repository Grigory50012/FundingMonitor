# CurrentDataTable

Таблица отображения текущих ставок финансирования с сортировкой по колонкам.

## Путь
`frontend/FundingMonitor.Web/src/components/CurrentDataTable.tsx`

## Data structure (FundingRateDto)

```typescript
{
  exchange: ExchangeType;      // "Binance" | "Bybit" | "OKX"
  symbol: string;              // "BTC-USDT"
  markPrice: number;
  fundingRate: number;
  apr: number;
  numberOfPaymentsPerDay: number;
  nextFundingTime: string | null;
  exchangeUrl: string;         // Link to the exchange futures page
}
```

## Props

```typescript
interface CurrentDataTableProps {
  data: FundingRateDto[];           // Массив DTO с текущими ставками
  selectedExchanges: ExchangeType[]; // Фильтр по биржам (пустой = все)
}
```

## Функциональность

### Сортировка
- Клик по заголовку колонки циклически переключает: `asc` → `desc` → `none` (оригинальный порядок)
- Сортируемые колонки: **Mark Price**, **Ставка**, **Время**, **APR**
- Иконка сортировки меняется в зависимости от направления

### Фильтрация
- Фильтрация по биржам происходит на уровне родителя (передается `selectedExchanges`)
- Внутри компонента только применяется фильтр

### Форматирование
| Поле | Формат |
|------|--------|
| Mark Price | `$` + locale string, 2-8 знаков после запятой |
| Funding Rate | `%` с 3 знаками, цвет: зелёный (+), красный (-), серый (0) |
| Payments/Day | `{N} вып./день` |
| Next Funding Time | `HH:MM` (локальное время) или `—` |
| APR | `XX.XX%`, цвет по знаку |

### UI детали
- Sticky заголовок (`position: sticky; top: 0`)
- Sticky первый столбец (Биржа) при горизонтальном скролле
- Цветные бейджи бирж: Binance (жёлтый), Bybit (оранжевый), OKX (серый)
- Пустое состояние: "Нет данных для отображения"

## Зависимости
- `FundingRateDto` из `../types`
- `ExchangeType` из `../types`

## Пример использования

```tsx
<CurrentDataTable
  data={currentRates}
  selectedExchanges={['Binance', 'Bybit']}
/>
```

## Производительность
- `useMemo` для отсортированных данных
- Ключи строк: `${exchange}-${symbol}`
- Минимальные ре-рендеры за счёт стабильных ссылок на функции
