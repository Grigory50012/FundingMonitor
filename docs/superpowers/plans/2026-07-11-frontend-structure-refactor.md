# Frontend Structure Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the React frontend structure without adding runtime dependencies and without changing the current data-loading model.

**Architecture:** Keep the Vite SPA and the current `useEffect`-based hooks. Split large mixed-responsibility components into small UI, feature, and shared utility modules while preserving the existing dashboard behavior and visual layout. Each task must leave `npm.cmd run build` passing; `npm.cmd run lint` should pass after Task 1 and remain passing.

**Tech Stack:** React 19, TypeScript strict mode, Vite 7, Tailwind CSS 4, Axios, Recharts, existing custom hooks.

## Global Constraints

- Do not add runtime dependencies.
- Do not migrate to TanStack Query, Zustand, Redux, Storybook, or a router in this refactor.
- Preserve current dashboard behavior: current rates, history chart/table toggle, APR stats table, arbitrage table, exchange links, localStorage filters, manual refresh buttons.
- Keep existing API endpoints and DTO shapes.
- Keep CSS variables based on `--tg-*` theme tokens.
- Use existing npm scripts only for verification: `npm.cmd run lint` and `npm.cmd run build`.
- Avoid broad visual redesign; structure first, visual cleanup only when it removes duplication or fixes broken text.

---

## File Structure

Create these new modules:

- `frontend/FundingMonitor.Web/src/shared/api/errors.ts`: typed extraction of API error messages.
- `frontend/FundingMonitor.Web/src/shared/lib/format.ts`: number, percent, price, and date formatting.
- `frontend/FundingMonitor.Web/src/shared/lib/symbols.ts`: symbol normalization helpers.
- `frontend/FundingMonitor.Web/src/shared/lib/sort.ts`: reusable tri-state sort helpers.
- `frontend/FundingMonitor.Web/src/shared/ui/Spinner.tsx`: common loading spinner.
- `frontend/FundingMonitor.Web/src/shared/ui/EmptyState.tsx`: common empty/error state.
- `frontend/FundingMonitor.Web/src/shared/ui/Panel.tsx`: common bordered dashboard panel.
- `frontend/FundingMonitor.Web/src/shared/ui/SortIcon.tsx`: common sort icon.
- `frontend/FundingMonitor.Web/src/entities/exchange/model.ts`: exchange constants, labels, and colors.
- `frontend/FundingMonitor.Web/src/entities/exchange/ExchangeBadge.tsx`: exchange badge.
- `frontend/FundingMonitor.Web/src/features/filters/CompactFilter.tsx`: move active filter component here.
- `frontend/FundingMonitor.Web/src/features/current-rates/CurrentRatesTable.tsx`: replacement boundary for current data table.
- `frontend/FundingMonitor.Web/src/features/history/HistoryChartPanel.tsx`: replacement boundary for history chart.
- `frontend/FundingMonitor.Web/src/features/history/HistoryAprTable.tsx`: replacement boundary for APR stats table.
- `frontend/FundingMonitor.Web/src/features/arbitrage/ArbitrageTable.tsx`: replacement boundary for arbitrage table.
- `frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx`: presentational dashboard layout.
- `frontend/FundingMonitor.Web/src/widgets/dashboard/useDashboardData.ts`: dashboard orchestration hook.

Modify these existing modules:

- `frontend/FundingMonitor.Web/src/App.tsx`: render `DashboardPage` or a container wrapper from `widgets/dashboard`.
- `frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx`: shrink to compatibility wrapper or delete after references move.
- `frontend/FundingMonitor.Web/src/hooks/useFundingRates.ts`: use typed error helper; keep hook signatures.
- `frontend/FundingMonitor.Web/src/components/index.ts`: export compatibility aliases during migration, then remove legacy exports.
- `frontend/FundingMonitor.Web/src/types/index.ts`: keep DTOs, move UI constants out.
- `frontend/FundingMonitor.Web/src/types/history.ts`: fix labels and keep time-range types.
- `frontend/FundingMonitor.Web/src/index.css`: keep tokens; only fix broken comments if touched.
- `frontend/FundingMonitor.Web/README.md`: update structure notes after migration.
- `docs/frontend/components/index.md`: update component map after migration.

---

### Task 1: Baseline Text, Lint, and Error Typing

**Files:**
- Create: `frontend/FundingMonitor.Web/src/shared/api/errors.ts`
- Modify: `frontend/FundingMonitor.Web/src/hooks/useFundingRates.ts`
- Modify: `frontend/FundingMonitor.Web/src/types/index.ts`
- Modify: `frontend/FundingMonitor.Web/src/types/history.ts`

**Interfaces:**
- Produces: `getApiErrorMessage(error: unknown, fallback: string): string`
- Produces: corrected Russian labels in `PERIODS` and `TIME_RANGES`
- Preserves: `useCurrentRates`, `useHistoryRates`, `useArbitrageRates` return shape

- [ ] **Step 1: Add typed API error helper**

Create `frontend/FundingMonitor.Web/src/shared/api/errors.ts`:

```ts
type ApiErrorLike = {
  message?: unknown;
  response?: {
    data?: {
      details?: unknown;
      error?: unknown;
    };
  };
};

const isObject = (value: unknown): value is Record<string, unknown> =>
  typeof value === "object" && value !== null;

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!isObject(error)) return fallback;

  const candidate = error as ApiErrorLike;
  const details = candidate.response?.data?.details;
  if (typeof details === "string" && details.trim().length > 0) {
    return details;
  }

  const apiError = candidate.response?.data?.error;
  if (typeof apiError === "string" && apiError.trim().length > 0) {
    return apiError;
  }

  if (typeof candidate.message === "string" && candidate.message.trim().length > 0) {
    return candidate.message;
  }

  return fallback;
}
```

- [ ] **Step 2: Replace `any` error extraction in hooks**

In `frontend/FundingMonitor.Web/src/hooks/useFundingRates.ts`, import:

```ts
import { getApiErrorMessage } from "../shared/api/errors";
```

Replace each catch message block with:

```ts
const msg = getApiErrorMessage(
  err,
  "Не удалось загрузить текущие данные",
);
```

Use these fallbacks:

```ts
"Не удалось загрузить текущие данные"
"Не удалось загрузить исторические данные"
"Не удалось загрузить арбитражные данные"
```

- [ ] **Step 3: Fix static Russian labels**

In `frontend/FundingMonitor.Web/src/types/index.ts`, replace `PERIODS` with:

```ts
export const PERIODS = [
  { label: "1 день", days: 1 },
  { label: "2 дня", days: 2 },
  { label: "3 дня", days: 3 },
  { label: "7 дней", days: 7 },
  { label: "14 дней", days: 14 },
  { label: "21 день", days: 21 },
  { label: "30 дней", days: 30 },
] as const;
```

In `frontend/FundingMonitor.Web/src/types/history.ts`, replace `TIME_RANGES` labels with:

```ts
export const TIME_RANGES: {
  value: TimeRangeType;
  label: string;
  days: number;
}[] = [
  { value: "1d", label: "1 день", days: 1 },
  { value: "2d", label: "2 дня", days: 2 },
  { value: "3d", label: "3 дня", days: 3 },
  { value: "1w", label: "1 неделя", days: 7 },
  { value: "2w", label: "2 недели", days: 14 },
  { value: "3w", label: "3 недели", days: 21 },
  { value: "1m", label: "1 месяц", days: 30 },
];
```

- [ ] **Step 4: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

Expected:

```text
lint: 0 errors
build: exits 0
```

- [ ] **Step 5: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/shared/api/errors.ts frontend/FundingMonitor.Web/src/hooks/useFundingRates.ts frontend/FundingMonitor.Web/src/types/index.ts frontend/FundingMonitor.Web/src/types/history.ts
git commit -m "refactor(frontend): type API error handling"
```

---

### Task 2: Shared Formatting, Sorting, and Exchange Model

**Files:**
- Create: `frontend/FundingMonitor.Web/src/shared/lib/format.ts`
- Create: `frontend/FundingMonitor.Web/src/shared/lib/symbols.ts`
- Create: `frontend/FundingMonitor.Web/src/shared/lib/sort.ts`
- Create: `frontend/FundingMonitor.Web/src/entities/exchange/model.ts`
- Modify: `frontend/FundingMonitor.Web/src/types/index.ts`

**Interfaces:**
- Produces: `formatPercent(value, options?)`
- Produces: `formatPrice(value, options?)`
- Produces: `formatTime(value)`
- Produces: `toBaseSymbol(symbol)` and `toUsdtSymbol(symbol)`
- Produces: `nextSortDirection(direction)`
- Produces: `EXCHANGES`, `EXCHANGE_COLORS`, `getExchangeTone(exchange)`

- [ ] **Step 1: Add format helpers**

Create `frontend/FundingMonitor.Web/src/shared/lib/format.ts`:

```ts
type FractionOptions = {
  minimumFractionDigits?: number;
  maximumFractionDigits?: number;
};

export function formatNumber(value: number, options: FractionOptions = {}): string {
  return value.toLocaleString(undefined, {
    minimumFractionDigits: options.minimumFractionDigits ?? 0,
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
  });
}

export function formatPercent(value: number, options: FractionOptions = {}): string {
  return `${formatNumber(value, {
    minimumFractionDigits: options.minimumFractionDigits ?? 2,
    maximumFractionDigits: options.maximumFractionDigits ?? 2,
  })}%`;
}

export function formatPrice(value: number, options: FractionOptions = {}): string {
  return `$${formatNumber(value, {
    minimumFractionDigits: options.minimumFractionDigits ?? 2,
    maximumFractionDigits: options.maximumFractionDigits ?? 8,
  })}`;
}

export function formatTime(value: string | null): string {
  if (!value) return "-";

  return new Date(value).toLocaleTimeString("ru-RU", {
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function getSignedColor(value: number): string {
  if (value > 0) return "var(--tg-positive)";
  if (value < 0) return "var(--tg-negative)";
  return "var(--tg-text-tertiary)";
}
```

- [ ] **Step 2: Add symbol helpers**

Create `frontend/FundingMonitor.Web/src/shared/lib/symbols.ts`:

```ts
export function toBaseSymbol(symbol: string): string {
  return symbol.replace("-USDT", "");
}

export function toUsdtSymbol(symbol: string): string {
  const trimmed = symbol.trim();
  if (!trimmed) return "";
  return trimmed.includes("-") ? trimmed : `${trimmed}-USDT`;
}
```

- [ ] **Step 3: Add sort helper**

Create `frontend/FundingMonitor.Web/src/shared/lib/sort.ts`:

```ts
export type SortDirection = "asc" | "desc" | null;

export function nextSortDirection(direction: SortDirection): SortDirection {
  if (direction === "asc") return "desc";
  if (direction === "desc") return null;
  return "asc";
}

export function compareNullableNumbers(
  a: number | null,
  b: number | null,
  direction: Exclude<SortDirection, null>,
): number {
  if (a === null && b === null) return 0;
  if (a === null) return 1;
  if (b === null) return -1;

  const multiplier = direction === "asc" ? 1 : -1;
  return (a - b) * multiplier;
}
```

- [ ] **Step 4: Move exchange UI constants**

Create `frontend/FundingMonitor.Web/src/entities/exchange/model.ts`:

```ts
import type { ExchangeType } from "../../types";

export const EXCHANGES: ExchangeType[] = ["Binance", "Bybit", "OKX"];

export const EXCHANGE_COLORS: Record<ExchangeType, string> = {
  Binance: "#f0760b",
  Bybit: "#ffcc00",
  OKX: "#FFFFFF",
};

export type ExchangeTone = {
  backgroundColor: string;
  color: string;
};

export function getExchangeTone(exchange: string): ExchangeTone {
  if (exchange === "Binance") {
    return { backgroundColor: "rgba(241, 196, 15, 0.15)", color: "#F1C40F" };
  }

  if (exchange === "Bybit") {
    return { backgroundColor: "rgba(230, 126, 34, 0.15)", color: "#E67E22" };
  }

  return {
    backgroundColor: "var(--tg-bg-tertiary)",
    color: "var(--tg-text-secondary)",
  };
}
```

Remove `EXCHANGE_COLORS` from `frontend/FundingMonitor.Web/src/types/index.ts` after imports are updated in later tasks.

- [ ] **Step 5: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 6: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/shared frontend/FundingMonitor.Web/src/entities frontend/FundingMonitor.Web/src/types/index.ts
git commit -m "refactor(frontend): add shared formatting and exchange helpers"
```

---

### Task 3: Shared UI Primitives

**Files:**
- Create: `frontend/FundingMonitor.Web/src/shared/ui/Spinner.tsx`
- Create: `frontend/FundingMonitor.Web/src/shared/ui/EmptyState.tsx`
- Create: `frontend/FundingMonitor.Web/src/shared/ui/Panel.tsx`
- Create: `frontend/FundingMonitor.Web/src/shared/ui/SortIcon.tsx`
- Create: `frontend/FundingMonitor.Web/src/entities/exchange/ExchangeBadge.tsx`

**Interfaces:**
- Produces: `Spinner({ className? })`
- Produces: `EmptyState({ children, tone? })`
- Produces: `Panel({ children, className? })`
- Produces: `SortIcon({ direction, className? })`
- Produces: `ExchangeBadge({ exchange, compact? })`

- [ ] **Step 1: Add loading primitive**

Create `frontend/FundingMonitor.Web/src/shared/ui/Spinner.tsx`:

```tsx
type SpinnerProps = {
  className?: string;
};

export function Spinner({ className = "w-12 h-12" }: SpinnerProps) {
  return (
    <div
      className={`${className} border-4 rounded-full animate-spin`}
      style={{
        borderColor: "var(--tg-border)",
        borderTopColor: "var(--tg-button)",
      }}
    />
  );
}
```

- [ ] **Step 2: Add empty/error state primitive**

Create `frontend/FundingMonitor.Web/src/shared/ui/EmptyState.tsx`:

```tsx
import type { ReactNode } from "react";

type EmptyStateProps = {
  children: ReactNode;
  tone?: "muted" | "danger";
  className?: string;
};

export function EmptyState({ children, tone = "muted", className = "" }: EmptyStateProps) {
  return (
    <div
      className={`flex items-center justify-center h-full ${className}`}
      style={{
        color: tone === "danger" ? "var(--tg-negative)" : "var(--tg-text-tertiary)",
      }}
    >
      <p className="text-xs">{children}</p>
    </div>
  );
}
```

- [ ] **Step 3: Add panel primitive**

Create `frontend/FundingMonitor.Web/src/shared/ui/Panel.tsx`:

```tsx
import type { ReactNode } from "react";

type PanelProps = {
  children: ReactNode;
  className?: string;
};

export function Panel({ children, className = "" }: PanelProps) {
  return (
    <section
      className={`rounded-2xl overflow-hidden ${className}`}
      style={{
        backgroundColor: "var(--tg-bg-secondary)",
        border: "1px solid var(--tg-border)",
      }}
    >
      {children}
    </section>
  );
}
```

- [ ] **Step 4: Add sort icon primitive**

Create `frontend/FundingMonitor.Web/src/shared/ui/SortIcon.tsx`:

```tsx
import type { SortDirection } from "../lib/sort";

type SortIconProps = {
  direction: SortDirection;
  className?: string;
};

export function SortIcon({ direction, className = "w-3 h-3 flex-shrink-0" }: SortIconProps) {
  if (direction === "asc") {
    return (
      <svg className={`${className} text-blue-400`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
      </svg>
    );
  }

  if (direction === "desc") {
    return (
      <svg className={`${className} text-blue-400`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
      </svg>
    );
  }

  return (
    <svg className={`${className} text-gray-600`} fill="none" stroke="currentColor" viewBox="0 0 24 24">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16V4m0 0L3 8m4-4l4 4m6 0v12m0 0l4-4m-4 4l-4-4" />
    </svg>
  );
}
```

- [ ] **Step 5: Add exchange badge**

Create `frontend/FundingMonitor.Web/src/entities/exchange/ExchangeBadge.tsx`:

```tsx
import { getExchangeTone } from "./model";

type ExchangeBadgeProps = {
  exchange: string;
  compact?: boolean;
};

export function ExchangeBadge({ exchange, compact = false }: ExchangeBadgeProps) {
  const tone = getExchangeTone(exchange);

  return (
    <span
      className={`${compact ? "px-1.5 py-0.5 text-[10px]" : "px-2 py-1 text-xs"} rounded-md font-semibold inline-block text-center`}
      style={tone}
    >
      {exchange}
    </span>
  );
}
```

- [ ] **Step 6: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 7: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/shared/ui frontend/FundingMonitor.Web/src/entities/exchange/ExchangeBadge.tsx
git commit -m "refactor(frontend): add shared UI primitives"
```

---

### Task 4: Move and Clean Active Filter

**Files:**
- Create: `frontend/FundingMonitor.Web/src/features/filters/CompactFilter.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/CompactFilter.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/index.ts`
- Modify: `frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx`

**Interfaces:**
- Produces: `CompactFilterProps`
- Preserves: `selectedExchanges`, `onExchangesChange`, `selectedSymbol`, `onSymbolChange`, `availableSymbols`

- [ ] **Step 1: Move implementation**

Copy the current `CompactFilter` implementation to `frontend/FundingMonitor.Web/src/features/filters/CompactFilter.tsx`.

Replace hardcoded exchange list with:

```ts
import { EXCHANGES } from "../../entities/exchange/model";
```

Change labels to valid Russian text:

```ts
if (selectedExchanges.length === 0) return "Все биржи";
if (selectedExchanges.length === EXCHANGES.length) return "Все биржи";
```

Use these visible strings:

```text
Поиск монеты...
Сбросить
Все
Сброс
```

- [ ] **Step 2: Keep compatibility export**

Replace `frontend/FundingMonitor.Web/src/components/CompactFilter.tsx` with:

```tsx
export { CompactFilter } from "../features/filters/CompactFilter";
```

- [ ] **Step 3: Update direct dashboard import**

In `frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx`, import `CompactFilter` from:

```ts
import { CompactFilter } from "../features/filters/CompactFilter";
```

Keep table imports from `../components` until their tasks move them.

- [ ] **Step 4: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 5: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/features/filters frontend/FundingMonitor.Web/src/components/CompactFilter.tsx frontend/FundingMonitor.Web/src/components/index.ts frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx
git commit -m "refactor(frontend): move compact filter to feature layer"
```

---

### Task 5: Split Dashboard Orchestration from Layout

**Files:**
- Create: `frontend/FundingMonitor.Web/src/widgets/dashboard/useDashboardData.ts`
- Create: `frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx`
- Modify: `frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx`
- Modify: `frontend/FundingMonitor.Web/src/App.tsx`

**Interfaces:**
- Produces: `useDashboardData()`
- Produces: `DashboardPage()`
- Preserves: localStorage keys from `STORAGE_KEYS`

- [ ] **Step 1: Create dashboard data hook**

Move non-JSX dashboard logic into `frontend/FundingMonitor.Web/src/widgets/dashboard/useDashboardData.ts`:

```ts
import { useCallback, useEffect, useMemo, useState } from "react";
import { fundingRatesApi } from "../../api/fundingRates";
import { STORAGE_KEYS } from "../../config/storageKeys";
import { useCurrentRates, useHistoryRates, useArbitrageRates } from "../../hooks/useFundingRates";
import { useLocalStorage } from "../../hooks/useLocalStorage";
import { toUsdtSymbol } from "../../shared/lib/symbols";
import type { ExchangeType } from "../../types";

const DEFAULT_COINS = ["BTC", "ETH", "SOL", "XRP", "DOGE"];

export type HistoryViewMode = "chart" | "table";

export type FilterState = {
  exchanges: ExchangeType[];
  symbol: string;
};

export function useDashboardData() {
  const [mainFilters, setMainFilters] = useLocalStorage<FilterState>(STORAGE_KEYS.mainFilters, {
    exchanges: [],
    symbol: "BTC",
  });
  const [arbitrageFilters, setArbitrageFilters] = useLocalStorage<FilterState>(
    STORAGE_KEYS.arbitrageFilters,
    { exchanges: [], symbol: "" },
  );
  const [allCoins, setAllCoins] = useState<string[]>([]);
  const [historyViewMode, setHistoryViewMode] = useState<HistoryViewMode>("chart");
  const [timeRange, setTimeRange] = useState<"1d" | "2d" | "3d" | "1w" | "2w" | "3w" | "1m">("1d");

  const current = useCurrentRates({
    symbol: mainFilters.symbol,
    exchanges: mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
  });

  const history = useHistoryRates({
    symbol: toUsdtSymbol(mainFilters.symbol),
    exchanges: mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
    limit: 1000,
  });

  const arbitrage = useArbitrageRates({
    symbol: arbitrageFilters.symbol || undefined,
    exchanges: arbitrageFilters.exchanges.length > 0 ? arbitrageFilters.exchanges : undefined,
  });

  const loadAllCoins = useCallback(async () => {
    try {
      const data = await fundingRatesApi.getCurrentRates({});
      const coins = Array.from(new Set(data.map((item) => item.symbol.replace("-USDT", "")))).sort();
      setAllCoins(coins);
    } catch (err) {
      console.error("Failed to load all coins:", err);
    }
  }, []);

  useEffect(() => {
    loadAllCoins();
  }, [loadAllCoins]);

  const availableCoins = useMemo(
    () => Array.from(new Set([...DEFAULT_COINS, ...allCoins])).sort(),
    [allCoins],
  );

  return {
    mainFilters,
    setMainFilters,
    arbitrageFilters,
    setArbitrageFilters,
    current,
    history,
    arbitrage,
    availableCoins,
    historyViewMode,
    setHistoryViewMode,
    timeRange,
    setTimeRange,
    errorMessage: current.error ?? history.error ?? arbitrage.error ?? null,
  };
}
```

- [ ] **Step 2: Create dashboard page component**

Move the JSX from `DashboardContainer.tsx` into `frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx`.

At the top:

```tsx
import { ArbitrageTable, CurrentDataTable, HistoryPanel, HistoryTable } from "../../components";
import { CompactFilter } from "../../features/filters/CompactFilter";
import { Panel } from "../../shared/ui/Panel";
import { Spinner } from "../../shared/ui/Spinner";
import type { ExchangeType } from "../../types";
import { useDashboardData } from "./useDashboardData";
```

Replace repeated panel wrappers with:

```tsx
<Panel className="p-4 flex flex-col min-w-0">
  {children}
</Panel>
```

- [ ] **Step 3: Shrink container wrapper**

Replace `frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx` with:

```tsx
import { DashboardPage } from "../widgets/dashboard/DashboardPage";

const DashboardContainer = () => <DashboardPage />;

export default DashboardContainer;
```

- [ ] **Step 4: Keep App stable**

No behavior change is required in `App.tsx` yet. It can continue rendering `DashboardContainer`.

- [ ] **Step 5: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 6: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/widgets/dashboard frontend/FundingMonitor.Web/src/containers/DashboardContainer.tsx frontend/FundingMonitor.Web/src/App.tsx
git commit -m "refactor(frontend): split dashboard data from layout"
```

---

### Task 6: Refactor Current Rates Table

**Files:**
- Create: `frontend/FundingMonitor.Web/src/features/current-rates/CurrentRatesTable.tsx`
- Create: `frontend/FundingMonitor.Web/src/features/current-rates/currentRatesTableModel.ts`
- Modify: `frontend/FundingMonitor.Web/src/components/CurrentDataTable.tsx`
- Modify: `frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx`

**Interfaces:**
- Produces: `CurrentRatesTable({ data, selectedExchanges })`
- Produces: `sortCurrentRates(data, sortConfig)`
- Preserves: existing `CurrentDataTable` as compatibility alias

- [ ] **Step 1: Extract table model**

Create `frontend/FundingMonitor.Web/src/features/current-rates/currentRatesTableModel.ts`:

```ts
import type { FundingRateDto, ExchangeType } from "../../types";
import type { SortDirection } from "../../shared/lib/sort";
import { compareNullableNumbers } from "../../shared/lib/sort";

export type CurrentRatesSortColumn = "fundingRate" | "apr" | "markPrice" | "nextFundingTime";

export type CurrentRatesSortConfig = {
  column: CurrentRatesSortColumn;
  direction: SortDirection;
};

export function filterCurrentRates(data: FundingRateDto[], selectedExchanges: ExchangeType[]) {
  if (selectedExchanges.length === 0) return data;
  return data.filter((item) => selectedExchanges.includes(item.exchange));
}

export function sortCurrentRates(data: FundingRateDto[], sortConfig: CurrentRatesSortConfig) {
  if (!sortConfig.direction) return data;

  return [...data].sort((a, b) => {
    if (sortConfig.column === "fundingRate") {
      return compareNullableNumbers(a.fundingRate, b.fundingRate, sortConfig.direction!);
    }
    if (sortConfig.column === "apr") {
      return compareNullableNumbers(a.apr, b.apr, sortConfig.direction!);
    }
    if (sortConfig.column === "markPrice") {
      return compareNullableNumbers(a.markPrice, b.markPrice, sortConfig.direction!);
    }

    const valueA = a.nextFundingTime ? new Date(a.nextFundingTime).getTime() : null;
    const valueB = b.nextFundingTime ? new Date(b.nextFundingTime).getTime() : null;
    return compareNullableNumbers(valueA, valueB, sortConfig.direction!);
  });
}
```

- [ ] **Step 2: Create new table component**

Move `CurrentDataTable` implementation to `features/current-rates/CurrentRatesTable.tsx`.

Use:

```ts
import { ExchangeBadge } from "../../entities/exchange/ExchangeBadge";
import { formatPercent, formatPrice, formatTime, getSignedColor } from "../../shared/lib/format";
import { nextSortDirection } from "../../shared/lib/sort";
import { EmptyState } from "../../shared/ui/EmptyState";
import { SortIcon } from "../../shared/ui/SortIcon";
```

Replace inline exchange badge styling with:

```tsx
<ExchangeBadge exchange={item.exchange} compact />
```

Replace empty state with:

```tsx
<EmptyState>Нет данных для отображения</EmptyState>
```

- [ ] **Step 3: Add compatibility alias**

Replace `frontend/FundingMonitor.Web/src/components/CurrentDataTable.tsx` with:

```tsx
export { CurrentRatesTable as CurrentDataTable } from "../features/current-rates/CurrentRatesTable";
```

- [ ] **Step 4: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 5: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/features/current-rates frontend/FundingMonitor.Web/src/components/CurrentDataTable.tsx frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx
git commit -m "refactor(frontend): split current rates table"
```

---

### Task 7: Refactor History Chart and APR Table

**Files:**
- Create: `frontend/FundingMonitor.Web/src/features/history/historyChartModel.ts`
- Create: `frontend/FundingMonitor.Web/src/features/history/HistoryChartPanel.tsx`
- Create: `frontend/FundingMonitor.Web/src/features/history/useAprStats.ts`
- Create: `frontend/FundingMonitor.Web/src/features/history/HistoryAprTable.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/HistoryPanel.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/HistoryTable.tsx`

**Interfaces:**
- Produces: `HistoryChartPanel({ data, selectedExchanges, timeRange, onTimeRangeChange })`
- Produces: `useAprStats({ symbol, selectedExchanges })`
- Produces: `HistoryAprTable({ symbol, selectedExchanges })`
- Preserves: compatibility exports as `HistoryPanel` and `HistoryTable`

- [ ] **Step 1: Extract chart data shaping**

Create `frontend/FundingMonitor.Web/src/features/history/historyChartModel.ts` and move the data filtering/grouping code from `HistoryPanel.tsx` into:

```ts
import type { HistoricalFundingRateDto, ExchangeType } from "../../types";
import type { TimeRangeType } from "../../types/history";
import { TIME_RANGES } from "../../types/history";

export function filterHistoryByExchange(data: HistoricalFundingRateDto[], selectedExchanges: ExchangeType[]) {
  if (selectedExchanges.length === 0) return data;
  return data.filter((item) => selectedExchanges.includes(item.exchange));
}

export function filterHistoryByTimeRange(data: HistoricalFundingRateDto[], timeRange: TimeRangeType) {
  const selectedRange = TIME_RANGES.find((range) => range.value === timeRange);
  if (!selectedRange) return data;

  const cutoffDate = new Date(Date.now() - selectedRange.days * 24 * 60 * 60 * 1000);
  return data.filter((item) => new Date(item.fundingTime) >= cutoffDate);
}
```

Then add `buildHistoryChartData(data)` by moving the existing `chartData` `useMemo` body into a pure function. Keep the current chart shape unchanged.

- [ ] **Step 2: Move chart component**

Move `HistoryPanel.tsx` implementation to `features/history/HistoryChartPanel.tsx`.

Update imports:

```ts
import { EXCHANGE_COLORS } from "../../entities/exchange/model";
import { EmptyState } from "../../shared/ui/EmptyState";
import { buildHistoryChartData, filterHistoryByExchange, filterHistoryByTimeRange } from "./historyChartModel";
```

- [ ] **Step 3: Extract APR stats hook**

Create `frontend/FundingMonitor.Web/src/features/history/useAprStats.ts`:

```ts
import { useEffect, useState } from "react";
import { fundingRatesApi } from "../../api/fundingRates";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { AprPeriodStatsDto, ExchangeType } from "../../types";

type UseAprStatsParams = {
  symbol: string;
  selectedExchanges: ExchangeType[];
};

export function useAprStats({ symbol, selectedExchanges }: UseAprStatsParams) {
  const [data, setData] = useState<AprPeriodStatsDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!symbol) {
      setData([]);
      return;
    }

    let isActive = true;

    const loadData = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const stats = await fundingRatesApi.getAprStats({
          symbol,
          exchanges: selectedExchanges.length > 0 ? selectedExchanges : undefined,
        });
        if (isActive) setData(stats);
      } catch (err) {
        if (isActive) setError(getApiErrorMessage(err, "Ошибка загрузки данных"));
      } finally {
        if (isActive) setIsLoading(false);
      }
    };

    loadData();

    return () => {
      isActive = false;
    };
  }, [symbol, selectedExchanges]);

  return { data, isLoading, error };
}
```

- [ ] **Step 4: Move APR table**

Move `HistoryTable.tsx` implementation to `features/history/HistoryAprTable.tsx`.

Replace local API loading state with:

```ts
const { data, isLoading, error } = useAprStats({ symbol, selectedExchanges });
```

Use shared primitives:

```ts
import { ExchangeBadge } from "../../entities/exchange/ExchangeBadge";
import { formatPercent, getSignedColor } from "../../shared/lib/format";
import { nextSortDirection } from "../../shared/lib/sort";
import { EmptyState } from "../../shared/ui/EmptyState";
import { SortIcon } from "../../shared/ui/SortIcon";
```

- [ ] **Step 5: Add compatibility aliases**

Replace `frontend/FundingMonitor.Web/src/components/HistoryPanel.tsx` with:

```tsx
export { HistoryChartPanel as HistoryPanel } from "../features/history/HistoryChartPanel";
```

Replace `frontend/FundingMonitor.Web/src/components/HistoryTable.tsx` with:

```tsx
export { HistoryAprTable as HistoryTable } from "../features/history/HistoryAprTable";
```

- [ ] **Step 6: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 7: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/features/history frontend/FundingMonitor.Web/src/components/HistoryPanel.tsx frontend/FundingMonitor.Web/src/components/HistoryTable.tsx
git commit -m "refactor(frontend): split history chart and APR table"
```

---

### Task 8: Refactor Arbitrage Table

**Files:**
- Create: `frontend/FundingMonitor.Web/src/features/arbitrage/arbitrageTableModel.ts`
- Create: `frontend/FundingMonitor.Web/src/features/arbitrage/ArbitrageExchangeRows.tsx`
- Create: `frontend/FundingMonitor.Web/src/features/arbitrage/ArbitrageTable.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/ArbitrageTable.tsx`

**Interfaces:**
- Produces: `ArbitrageTable({ data, onArbitrageClick })`
- Produces: `groupArbitrageBySymbol(data, sortConfig)`
- Produces: `calcFundingSpread(item)` and `calcFundingRate(rate)`
- Preserves: existing row click behavior

- [ ] **Step 1: Extract arbitrage table model**

Create `frontend/FundingMonitor.Web/src/features/arbitrage/arbitrageTableModel.ts`:

```ts
import type { FundingArbitrageDto } from "../../types";
import type { SortDirection } from "../../shared/lib/sort";
import { toBaseSymbol } from "../../shared/lib/symbols";

export type ArbitrageSortColumn =
  | "profitabilityPercent"
  | "priceSpreadPercent"
  | "fundingRateSpread"
  | "symbol";

export type ArbitrageSortConfig = {
  column: ArbitrageSortColumn;
  direction: SortDirection;
};

export type SymbolGroup = {
  symbol: string;
  best: FundingArbitrageDto;
  others: FundingArbitrageDto[];
};

export const calcFundingSpread = (item: FundingArbitrageDto): number => {
  const spread = item.fundingRateSpread ?? (item.fundingRateA ?? 0) - (item.fundingRateB ?? 0);
  return spread * 100;
};

export const calcFundingRate = (rate: number): number => rate * 100;

export const getProfitability = (item: FundingArbitrageDto): number => Math.abs(item.aprSpread);

export function getArbitrageClickPayload(item: FundingArbitrageDto): [string, string[]] {
  return [toBaseSymbol(item.symbol), [item.exchangeA, item.exchangeB]];
}

export function sortArbitrage(data: FundingArbitrageDto[], sortConfig: ArbitrageSortConfig) {
  if (!sortConfig.direction) return data;

  const multiplier = sortConfig.direction === "asc" ? 1 : -1;

  return [...data].sort((a, b) => {
    if (sortConfig.column === "fundingRateSpread") {
      return (calcFundingSpread(a) - calcFundingSpread(b)) * multiplier;
    }
    if (sortConfig.column === "profitabilityPercent") {
      return (getProfitability(a) - getProfitability(b)) * multiplier;
    }
    if (sortConfig.column === "priceSpreadPercent") {
      return (a.priceSpreadPercent - b.priceSpreadPercent) * multiplier;
    }
    return sortConfig.direction === "asc"
      ? a.symbol.localeCompare(b.symbol)
      : b.symbol.localeCompare(a.symbol);
  });
}

export function groupArbitrageBySymbol(data: FundingArbitrageDto[]): SymbolGroup[] {
  const groups = new Map<string, FundingArbitrageDto[]>();

  for (const item of data) {
    const existing = groups.get(item.symbol);
    if (existing) existing.push(item);
    else groups.set(item.symbol, [item]);
  }

  return Array.from(groups.entries()).map(([symbol, items]) => ({
    symbol,
    best: items[0],
    others: items.slice(1),
  }));
}
```

- [ ] **Step 2: Extract row component**

Create `frontend/FundingMonitor.Web/src/features/arbitrage/ArbitrageExchangeRows.tsx` by moving the current `renderRow` JSX into a component:

```tsx
import { ExchangeBadge } from "../../entities/exchange/ExchangeBadge";
import { formatPercent, formatPrice, getSignedColor } from "../../shared/lib/format";
import type { FundingArbitrageDto } from "../../types";
import { calcFundingRate, calcFundingSpread, getArbitrageClickPayload } from "./arbitrageTableModel";

type ArbitrageExchangeRowsProps = {
  item: FundingArbitrageDto;
  showSymbol: boolean;
  onArbitrageClick?: (symbol: string, exchanges: string[]) => void;
};
```

Keep the current two-row layout and `rowSpan` behavior unchanged.

- [ ] **Step 3: Move table component**

Move the remaining table shell to `features/arbitrage/ArbitrageTable.tsx`.

Use:

```ts
import { nextSortDirection } from "../../shared/lib/sort";
import { EmptyState } from "../../shared/ui/EmptyState";
import { SortIcon } from "../../shared/ui/SortIcon";
import { groupArbitrageBySymbol, sortArbitrage } from "./arbitrageTableModel";
import { ArbitrageExchangeRows } from "./ArbitrageExchangeRows";
```

- [ ] **Step 4: Add compatibility alias**

Replace `frontend/FundingMonitor.Web/src/components/ArbitrageTable.tsx` with:

```tsx
export { ArbitrageTable } from "../features/arbitrage/ArbitrageTable";
```

- [ ] **Step 5: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 6: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/features/arbitrage frontend/FundingMonitor.Web/src/components/ArbitrageTable.tsx
git commit -m "refactor(frontend): split arbitrage table"
```

---

### Task 9: Remove Legacy Components and Clean Exports

**Files:**
- Modify: `frontend/FundingMonitor.Web/src/components/index.ts`
- Delete: `frontend/FundingMonitor.Web/src/components/CoinSelector.tsx`
- Delete: `frontend/FundingMonitor.Web/src/components/ExchangeSelector.tsx`
- Modify: `frontend/FundingMonitor.Web/README.md`
- Modify: `docs/frontend/components/index.md`

**Interfaces:**
- Removes unused legacy selectors.
- Preserves active public exports for dashboard components until all imports are updated.

- [ ] **Step 1: Confirm no imports remain**

Run:

```powershell
rg "CoinSelector|ExchangeSelector" frontend/FundingMonitor.Web/src
```

Expected:

```text
Only matches in components/index.ts and legacy component files.
```

- [ ] **Step 2: Remove legacy exports**

Edit `frontend/FundingMonitor.Web/src/components/index.ts` to:

```ts
export { CompactFilter } from "./CompactFilter";
export { CurrentDataTable } from "./CurrentDataTable";
export { HistoryPanel } from "./HistoryPanel";
export type { TimeRangeType } from "../types/history";
export { TIME_RANGES } from "../types/history";
export { HistoryTable } from "./HistoryTable";
export { ArbitrageTable } from "./ArbitrageTable";
```

- [ ] **Step 3: Delete unused components**

Remove:

```text
frontend/FundingMonitor.Web/src/components/CoinSelector.tsx
frontend/FundingMonitor.Web/src/components/ExchangeSelector.tsx
```

- [ ] **Step 4: Update docs**

In `frontend/FundingMonitor.Web/README.md` and `docs/frontend/components/index.md`:

- remove `CoinSelector` and `ExchangeSelector` from active component lists;
- add `shared/ui`, `shared/lib`, `entities/exchange`, `features/*`, and `widgets/dashboard` to structure sections;
- keep the note that no runtime state/data-fetching library is used.

- [ ] **Step 5: Verify**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

- [ ] **Step 6: Commit**

```powershell
git add frontend/FundingMonitor.Web/src/components frontend/FundingMonitor.Web/README.md docs/frontend/components/index.md
git commit -m "refactor(frontend): remove legacy selector components"
```

---

### Task 10: Final Import Cleanup and Bundle Check

**Files:**
- Modify: `frontend/FundingMonitor.Web/src/App.tsx`
- Modify: `frontend/FundingMonitor.Web/src/components/index.ts`
- Modify: `frontend/FundingMonitor.Web/src/widgets/dashboard/DashboardPage.tsx`
- Modify: `frontend/FundingMonitor.Web/src/types/index.ts`

**Interfaces:**
- App imports the dashboard from `widgets/dashboard`.
- Component compatibility aliases remain only if external docs require them.

- [ ] **Step 1: Point App at the new dashboard**

Change `frontend/FundingMonitor.Web/src/App.tsx` to:

```tsx
import { DashboardPage } from "./widgets/dashboard/DashboardPage";

const App = () => <DashboardPage />;

export default App;
```

- [ ] **Step 2: Search for stale imports**

Run:

```powershell
rg "../components|components/" frontend/FundingMonitor.Web/src
rg "EXCHANGE_COLORS" frontend/FundingMonitor.Web/src
```

Expected:

```text
No stale imports from ../components in feature/widget code.
EXCHANGE_COLORS imports only from entities/exchange/model.
```

- [ ] **Step 3: Remove compatibility wrappers if no longer needed**

If no app code imports from `src/components`, keep `components/index.ts` only as a public compatibility barrel:

```ts
export { CompactFilter } from "../features/filters/CompactFilter";
export { CurrentRatesTable as CurrentDataTable } from "../features/current-rates/CurrentRatesTable";
export { HistoryChartPanel as HistoryPanel } from "../features/history/HistoryChartPanel";
export { HistoryAprTable as HistoryTable } from "../features/history/HistoryAprTable";
export { ArbitrageTable } from "../features/arbitrage/ArbitrageTable";
export type { TimeRangeType } from "../types/history";
export { TIME_RANGES } from "../types/history";
```

- [ ] **Step 4: Verify final checks**

Run:

```powershell
npm.cmd run lint
npm.cmd run build
```

Record the final build size shown by Vite. The Recharts chunk warning may remain because no runtime dependency or routing/lazy-loading change is included in this plan.

- [ ] **Step 5: Commit**

```powershell
git add frontend/FundingMonitor.Web/src
git commit -m "refactor(frontend): clean dashboard imports"
```

---

## Deferred Work

These are intentionally out of scope for this plan:

- TanStack Query migration.
- Adding `lucide-react`.
- Adding Storybook.
- Adding Playwright.
- Adding Vitest and React Testing Library.
- Visual redesign of the dashboard.
- API DTO code generation from OpenAPI.

## Self-Review

- Spec coverage: The plan covers structure-only refactor, current hooks, no runtime dependencies, text cleanup, lint cleanup, shared utilities, dashboard split, table split, legacy cleanup, and final verification.
- Placeholder scan: Passed; no incomplete marker or unspecified implementation step remains.
- Type consistency: `SortDirection`, `ExchangeType`, `TimeRangeType`, and table sort config names are defined before use.
- Risk note: Task 8 is the highest-risk task because `ArbitrageTable.tsx` is large and has row-span layout. Implement it after Tasks 1-7 are passing.

## Execution Choice

Plan complete and saved to `docs/superpowers/plans/2026-07-11-frontend-structure-refactor.md`.

Two execution options:

1. Subagent-Driven (recommended) - dispatch a fresh subagent per task, review between tasks, fast iteration.
2. Inline Execution - execute tasks in this session using executing-plans, batch execution with checkpoints.
