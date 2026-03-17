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

export interface ApiErrorResponse {
  error: string;
  details?: string;
  requestId?: string;
}
