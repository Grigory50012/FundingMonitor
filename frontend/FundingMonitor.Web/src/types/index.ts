export type ExchangeType = "Binance" | "Bybit" | "OKX";

export interface FundingRateDto {
  exchange: ExchangeType;
  symbol: string;
  markPrice: number;
  fundingRate: number;
  apr: number;
  numberOfPaymentsPerDay: number;
  nextFundingTime: string | null;
}

export interface HistoricalFundingRateDto {
  exchange: ExchangeType;
  symbol: string;
  fundingRate: number;
  fundingTime: string;
}

export interface AprPeriodStatsDto {
  exchange: ExchangeType;
  period: string;
  days: number;
  apr: number;
  totalFundingRatePercent: number;
  paymentsCount: number;
  avgFundingRatePercent: number;
}

export interface ApiErrorResponse {
  error: string;
  details?: string;
  requestId?: string;
}

export interface FundingArbitrageDto {
  symbol: string;
  exchangeA: string;
  exchangeB: string;
  priceA: number;
  priceB: number;
  priceSpread: number;
  priceSpreadPercent: number;
  fundingRateA: number;
  fundingRateB: number;
  aprFundingRateA: number;
  aprFundingRateB: number;
  aprSpread: number;
  fundingRateSpread: number;
  paymentsA: number;
  paymentsB: number;
  shortExchange: string;
  longExchange: string;
}

export const PERIODS = [
  { label: "1 день", days: 1 },
  { label: "2 дня", days: 2 },
  { label: "3 дня", days: 3 },
  { label: "7 дней", days: 7 },
  { label: "14 дней", days: 14 },
  { label: "21 день", days: 21 },
  { label: "30 дней", days: 30 },
] as const;

export const EXCHANGE_COLORS: Record<ExchangeType, string> = {
  Binance: "#f0760b",
  Bybit: "#ffcc00",
  OKX: "#FFFFFF",
};
