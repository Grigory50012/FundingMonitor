import type { SortDirection } from "../../shared/lib/sort";

export type CurrentRatesSortColumn =
  | "fundingRate"
  | "apr"
  | "markPrice"
  | "nextFundingTime";

export type CurrentRatesSortConfig = {
  column: CurrentRatesSortColumn;
  direction: SortDirection;
};
