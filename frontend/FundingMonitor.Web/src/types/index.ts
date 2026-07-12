import type { components } from "./generated/api";

export type ExchangeType = "Binance" | "Bybit" | "OKX";

type ApiSchemas = components["schemas"];
type WithExchangeType<T extends { exchange: string }> = Omit<T, "exchange"> & {
  exchange: ExchangeType;
};

export type FundingRateDto = WithExchangeType<ApiSchemas["FundingRateDto"]>;
export type HistoricalFundingRateDto = WithExchangeType<
  ApiSchemas["HistoricalFundingRateDto"]
>;
export type AprPeriodStatsDto = WithExchangeType<
  ApiSchemas["AprPeriodStatsDto"]
>;

export interface ApiErrorResponse {
  error: string;
  details?: string;
  requestId?: string;
}

export type FundingArbitrageDto = ApiSchemas["FundingArbitrageDto"];

export const PERIODS = [
  { label: "1 день", days: 1 },
  { label: "2 дня", days: 2 },
  { label: "3 дня", days: 3 },
  { label: "7 дней", days: 7 },
  { label: "14 дней", days: 14 },
  { label: "21 день", days: 21 },
  { label: "30 дней", days: 30 },
] as const;
