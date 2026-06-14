# Frontend Components Documentation

Каталог с документацией React компонентов проекта FundingMonitor.Web.

## Архитектура UI

| Слой | Файл | Назначение |
|------|------|------------|
| **Container** | `containers/DashboardContainer.tsx` | Оркестрация данных, фильтров и панелей |
| **Hooks** | `hooks/useFundingRates.ts` | `useCurrentRates`, `useHistoryRates`, `useArbitrageRates` |
| **Hooks** | `hooks/useLocalStorage.ts` | Персистентность фильтров в localStorage |

## Компоненты

| Компонент | Файл | Назначение |
|-----------|------|------------|
| [CurrentDataTable](CurrentDataTable.md) | `CurrentDataTable.tsx` | Таблица текущих ставок финансирования с сортировкой |
| [HistoryTable](HistoryTable.md) | `HistoryTable.tsx` | Таблица APR статистики по периодам |
| [HistoryPanel](HistoryPanel.md) | `HistoryPanel.tsx` | График истории ставок (Recharts) + переключатель временных диапазонов |
| [ArbitrageTable](ArbitrageTable.md) | `ArbitrageTable.tsx` | Таблица арбитражных возможностей с группировкой по символам |
| [CompactFilter](CompactFilter.md) | `CompactFilter.tsx` | Компактный фильтр: поиск монеты + мультивыбор бирж (**используется в UI**) |
| [CoinSelector](CoinSelector.md) | `CoinSelector.tsx` | Выпадающий селектор монеты с поиском *(legacy, не используется)* |
| [ExchangeSelector](ExchangeSelector.md) | `ExchangeSelector.tsx` | Чекбокс-стиль селектор бирж *(legacy, заменён CompactFilter)* |

## Общие принципы

- **Стилизация**: Tailwind CSS 4 + CSS переменные (`var(--tg-*)`) для темизации
- **Типизация**: TypeScript strict mode, интерфейсы в `src/types/`
- **Состояние**: React hooks (`useState`, `useMemo`, `useCallback`, `useEffect`)
- **Доступность**: семантический HTML, фокус-стили, клавиатурная навигация
- **Адаптивность**: mobile-first, flex/grid, sticky headers для таблиц

## Цветовые константы (CSS переменные)

```css
:root {
  --tg-bg: #111827;           /* Основной фон */
  --tg-bg-secondary: #1F2937; /* Фон карточек/панелей */
  --tg-bg-tertiary: #374151;  /* Фон инпутов/селектов */
  --tg-border: #374151;       /* Границы */
  --tg-text: #F9FAFB;         /* Основной текст */
  --tg-text-secondary: #9CA3AF;
  --tg-text-tertiary: #6B7280;
  --tg-positive: #10B981;     /* Зеленый (профит, лонг) */
  --tg-negative: #EF4444;     /* Красный (убыток, шорт) */
  --tg-button: #0D9488;       /* Акцент (teal) */
  --tg-button-text: #FFFFFF;
  --tg-hint: #6B7280;
  --tg-link: #06B6D4;         /* Ссылки (cyan) */
}
```

Биржевые цвета (из `src/types/index.ts` → `EXCHANGE_COLORS`):
- **Binance**: `#f0760b` (оранжевый)
- **Bybit**: `#ffcc00` (жёлтый)
- **OKX**: `#FFFFFF` (белый)