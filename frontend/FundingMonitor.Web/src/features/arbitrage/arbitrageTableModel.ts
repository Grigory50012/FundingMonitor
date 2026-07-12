import { toBaseSymbol } from "../../shared/lib/symbols";
import type { SortDirection } from "../../shared/lib/sort";
import type { FundingArbitrageDto } from "../../types";

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
  return item.fundingRateSpread * 100;
};

export const calcFundingRate = (rate: number): number => rate * 100;

export const getProfitability = (item: FundingArbitrageDto): number =>
  item.aprSpread;

export function getArbitrageClickPayload(
  item: FundingArbitrageDto,
): [string, string[]] {
  return [toBaseSymbol(item.symbol), [item.exchangeA, item.exchangeB]];
}

export function sortArbitrage(
  data: FundingArbitrageDto[],
  sortConfig: ArbitrageSortConfig,
) {
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

export function groupArbitrageBySymbol(
  data: FundingArbitrageDto[],
): SymbolGroup[] {
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
