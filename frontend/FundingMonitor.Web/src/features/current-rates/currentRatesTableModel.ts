import { compareNullableNumbers } from "../../shared/lib/sort";
import type { SortDirection } from "../../shared/lib/sort";
import type { ExchangeType, FundingRateDto } from "../../types";

export type CurrentRatesSortColumn =
  | "fundingRate"
  | "apr"
  | "markPrice"
  | "nextFundingTime";

export type CurrentRatesSortConfig = {
  column: CurrentRatesSortColumn;
  direction: SortDirection;
};

export function filterCurrentRates(
  data: FundingRateDto[],
  selectedExchanges: ExchangeType[],
) {
  if (selectedExchanges.length === 0) return data;
  return data.filter((item) => selectedExchanges.includes(item.exchange));
}

export function sortCurrentRates(
  data: FundingRateDto[],
  sortConfig: CurrentRatesSortConfig,
) {
  const direction = sortConfig.direction;
  if (!direction) return data;

  return [...data].sort((a, b) => {
    if (sortConfig.column === "fundingRate") {
      return compareNullableNumbers(a.fundingRate, b.fundingRate, direction);
    }
    if (sortConfig.column === "apr") {
      return compareNullableNumbers(a.apr, b.apr, direction);
    }
    if (sortConfig.column === "markPrice") {
      return compareNullableNumbers(a.markPrice, b.markPrice, direction);
    }

    const valueA = a.nextFundingTime
      ? new Date(a.nextFundingTime).getTime()
      : null;
    const valueB = b.nextFundingTime
      ? new Date(b.nextFundingTime).getTime()
      : null;
    return compareNullableNumbers(valueA, valueB, direction);
  });
}
