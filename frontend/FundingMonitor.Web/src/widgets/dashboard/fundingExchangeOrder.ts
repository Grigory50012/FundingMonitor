import type { AprPeriodStatsDto, ExchangeType, FundingRateDto } from "../../types";
import type { CurrentRatesSortConfig } from "../../features/current-rates/currentRatesTableModel";
import type { HistoryAprSortConfig } from "../../features/historical-rates/HistoryAprTable";

export type FundingExchangeSort =
  | { source: "current"; config: CurrentRatesSortConfig }
  | { source: "history"; config: HistoryAprSortConfig };

export function getFundingExchangeOrder(
  current: FundingRateDto[],
  history: AprPeriodStatsDto[],
  selected: ExchangeType[],
  sort: FundingExchangeSort,
): ExchangeType[] {
  const exchanges = [...new Set([
    ...selected, ...current.map((row) => row.exchange), ...history.map((row) => row.exchange),
  ])].filter((exchange) => !selected.length || selected.includes(exchange)).sort();
  const direction = sort.config.direction;
  if (!direction) return exchanges;

  const value = (exchange: ExchangeType): number | null => {
    if (sort.source === "history") {
      const row = history.find((item) => item.exchange === exchange && item.period === sort.config.period);
      return row?.[sort.config.column] ?? null;
    }
    const row = current.find((item) => item.exchange === exchange);
    if (!row) return null;
    if (sort.config.column === "nextFundingTime") {
      if (!row.nextFundingTime) return null;
      const timestamp = Date.parse(row.nextFundingTime);
      return Number.isFinite(timestamp) ? timestamp : null;
    }
    return row[sort.config.column];
  };
  return exchanges.sort((a, b) => {
    const first = value(a);
    const second = value(b);
    if (first === null) return second === null ? a.localeCompare(b) : 1;
    if (second === null) return -1;
    return (first - second) * (direction === "asc" ? 1 : -1) || a.localeCompare(b);
  });
}
