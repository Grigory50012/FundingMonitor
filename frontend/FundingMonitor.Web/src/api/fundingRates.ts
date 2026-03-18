import axios from "axios";
import type {
  FundingRateDto,
  HistoricalFundingRateDto,
  ExchangeType,
  AprPeriodStatsDto,
} from "../types";

const API_BASE_URL = "/api/v1";

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

export const fundingRatesApi = {
  getCurrentRates: async (params?: {
    symbol?: string;
    exchanges?: ExchangeType[];
    includeInactive?: boolean;
  }): Promise<FundingRateDto[]> => {
    const queryParams = new URLSearchParams();

    if (params?.symbol) queryParams.append("symbol", params.symbol);
    if (params?.exchanges?.length)
      queryParams.append("exchanges", params.exchanges.join(","));
    if (params?.includeInactive !== undefined)
      queryParams.append("includeInactive", params.includeInactive.toString());

    const response = await apiClient.get<FundingRateDto[]>(
      `/FundingRates?${queryParams.toString()}`,
    );
    console.log("getCurrentRates response:", response.data);
    return Array.isArray(response.data) ? response.data : [];
  },

  getHistory: async (params: {
    symbol: string;
    exchanges?: ExchangeType[];
    from?: string;
    to?: string;
    limit?: number;
  }): Promise<HistoricalFundingRateDto[]> => {
    const queryParams = new URLSearchParams();

    queryParams.append("symbol", params.symbol);
    if (params.exchanges?.length)
      queryParams.append("exchanges", params.exchanges.join(","));
    if (params.from) queryParams.append("from", params.from);
    if (params.to) queryParams.append("to", params.to);
    if (params.limit)
      queryParams.append("limit", Math.min(params.limit, 1000).toString());

    const response = await apiClient.get<HistoricalFundingRateDto[]>(
      `/History?${queryParams.toString()}`,
    );
    return Array.isArray(response.data) ? response.data : [];
  },

  getAprStats: async (params: {
    symbol: string;
    exchanges?: ExchangeType[];
    periods?: number[];
  }): Promise<AprPeriodStatsDto[]> => {
    const queryParams = new URLSearchParams();

    queryParams.append("symbol", params.symbol);
    if (params.exchanges?.length)
      queryParams.append("exchanges", params.exchanges.join(","));
    if (params.periods?.length)
      queryParams.append("periods", params.periods.join(","));

    const response = await apiClient.get<AprPeriodStatsDto[]>(
      `/History/apr-stats?${queryParams.toString()}`,
    );
    return Array.isArray(response.data) ? response.data : [];
  },
};
