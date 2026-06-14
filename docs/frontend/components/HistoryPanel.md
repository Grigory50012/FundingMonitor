# HistoryPanel

График истории ставок финансирования (Recharts) с переключателем временных диапазонов и таблицей последних значений.

## Путь
`frontend/FundingMonitor.Web/src/components/HistoryPanel.tsx`

## Props

```typescript
interface HistoryPanelProps {
  data: HistoricalFundingRateDto[];      // Исторические данные (уже загруженные)
  selectedExchanges: ExchangeType[];      // Фильтр по биржам
  timeRange?: TimeRangeType;              // Текущий выбранный диапазон (default: "1m")
  onTimeRangeChange?: (range: TimeRangeType) => void; // Callback при смене диапазона
}
```

## Типы временных диапазонов

```typescript
type TimeRangeType = "1d" | "2d" | "3d" | "1w" | "2w" | "3w" | "1m";

const TIME_RANGES = [
  { value: "1d", label: "1 день", days: 1 },
  { value: "2d", label: "2 дня", days: 2 },
  { value: "3d", label: "3 дня", days: 3 },
  { value: "1w", label: "1 неделя", days: 7 },
  { value: "2w", label: "2 недели", days: 14 },
  { value: "3w", label: "3 недели", days: 21 },
  { value: "1m", label: "1 месяц", days: 30 },
] as const;
```

## Функциональность

### Агрегация данных для графика
1. **Фильтрация** по `selectedExchanges` и `timeRange`
2. **Группировка по часовым интервалам** (UTC) — объединяет данные разных бирж в одни точки времени
3. Для каждой биржи в интервале берётся **последнее значение** (latest funding rate)
4. Определяется **первая точка каждого дня** для подписи оси X (месяц+день)

### График (Recharts LineChart)
- **Линии**: одна на биржу (`type="monotone"`, `strokeWidth=2.5`, `connectNulls=true`)
- **Цвета**: из `EXCHANGE_COLORS` (Binance=оранжевый, Bybit=жёлтый, OKX=белый)
- **Ось X**: время (часовые метки), подписи только у первой точки дня, угол -45°
- **Ось Y**: ставка в %, формат `XX.XX%`
- **Сетка**: пунктирная `#4B5563`
- **Нулевая линия**: пунктирная ReferenceLine на Y=0
- **Легенда**: названия бирж
- **Анимация**: 500ms при смене диапазона (`key={timeRange}`)

### Кастомный Tooltip
- Показывает **все биржи** (даже если в payload пришла только одна)
- Дата/время из `rawTime` (UTC → локаль)
- Значения с 4 знаками после запятой
- Цвет значения: зелёный (+), красный (-), серый (0)
- Цветной маркер бировки слева

### Таблица "Последние значения"
- Под графиком, 3 колонки (по биржам)
- Последнее значение в выбранном диапазоне для каждой биржи
- Формат: `X.XXXX%` с цветом по знаку

### Переключатель диапазонов
- Кнопки из `TIME_RANGES`
- Активная: `var(--tg-button)` фон, остальные: `var(--tg-bg-tertiary)`
- Виден только если передан `onTimeRangeChange`

## Зависимости
- `recharts`: `LineChart`, `Line`, `XAxis`, `YAxis`, `CartesianGrid`, `Tooltip`, `Legend`, `ResponsiveContainer`, `ReferenceLine`
- `HistoricalFundingRateDto`, `ExchangeType`, `TimeRangeType` из `../types` и `../types/history`
- `EXCHANGE_COLORS` из `../types`
- `TIME_RANGES` из `../types/history`

## Пример использования

```tsx
<HistoryPanel
  data={historyData}
  selectedExchanges={['Binance', 'Bybit']}
  timeRange="1m"
  onTimeRangeChange={setTimeRange}
/>
```

## Производительность
- `useMemo` для `timeFilteredData` и `chartData` — тяжелые вычисления только при изменении данных/диапазона
- `key={timeRange}` на LineChart форсирует ремаунт при смене диапазона (чистая анимация)
- `connectNulls=true` — линии не рвутся при пропусках данных