export type ExchangeType = "Binance" | "Bybit" | "OKX";

export interface FundingRateDto {
  exchange: ExchangeType;
  symbol: string;
  markPrice: number;
  fundingRate: number;
  apr: number;
  numberOfPaymentsPerDay: number;
  nextFundingTime: string | null;
  exchangeUrl: string;
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
  stdDev: number;
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
  exchangeAUrl: string;
  exchangeBUrl: string;
}

export const PERIODS = [
  { label: "1 \u0434\u0435\u043d\u044c", days: 1 },
  { label: "2 \u0434\u043d\u044f", days: 2 },
  { label: "3 \u0434\u043d\u044f", days: 3 },
  { label: "7 \u0434\u043d\u0435\u0439", days: 7 },
  { label: "14 \u0434\u043d\u0435\u0439", days: 14 },
  { label: "21 \u0434\u0435\u043d\u044c", days: 21 },
  { label: "30 \u0434\u043d\u0435\u0439", days: 30 },
] as const;
