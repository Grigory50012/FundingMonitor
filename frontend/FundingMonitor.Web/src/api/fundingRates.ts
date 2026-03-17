import axios from "axios";
import type {
  FundingRateDto,
  HistoricalFundingRateDto,
  ExchangeType,
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

    const { data } = await apiClient.get<FundingRateDto[]>(
      `/FundingRates?${queryParams.toString()}`,
    );
    return data;
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

    const { data } = await apiClient.get<HistoricalFundingRateDto[]>(
      `/History?${queryParams.toString()}`,
    );
    return data;
  },
};
