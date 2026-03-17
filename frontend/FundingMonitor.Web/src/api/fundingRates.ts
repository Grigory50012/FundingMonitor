import axios from "axios";
import type {
  FundingRateDto,
  HistoricalFundingRateDto,
  ExchangeType,
} from "../types";

// Используем относительный путь (Vite proxy перенаправит на API)
const API_BASE_URL = "/api/v1";

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 10000,
});

// Добавляем перехватчик для отладки
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    console.error("API Error:", {
      url: error.config?.url,
      status: error.response?.status,
      data: error.response?.data,
      message: error.message,
    });
    return Promise.reject(error);
  },
);

export const fundingRatesApi = {
  /**
   * Получить текущие ставки финансирования
   */
  getCurrentRates: async (params?: {
    symbol?: string;
    exchanges?: ExchangeType[];
    includeInactive?: boolean;
  }): Promise<FundingRateDto[]> => {
    const queryParams = new URLSearchParams();

    if (params?.symbol) {
      queryParams.append("symbol", params.symbol);
    }

    if (params?.exchanges && params.exchanges.length > 0) {
      queryParams.append("exchanges", params.exchanges.join(","));
    }

    if (params?.includeInactive !== undefined) {
      queryParams.append("includeInactive", params.includeInactive.toString());
    }

    const response = await apiClient.get<FundingRateDto[]>(
      `/FundingRates?${queryParams.toString()}`,
    );
    return response.data;
  },

  /**
   * Получить исторические ставки финансирования
   */
  getHistory: async (params: {
    symbol: string;
    exchanges?: ExchangeType[];
    from?: string;
    to?: string;
    limit?: number;
  }): Promise<HistoricalFundingRateDto[]> => {
    const queryParams = new URLSearchParams();

    queryParams.append("symbol", params.symbol);

    if (params.exchanges && params.exchanges.length > 0) {
      queryParams.append("exchanges", params.exchanges.join(","));
    }

    if (params.from) {
      queryParams.append("from", params.from);
    }

    if (params.to) {
      queryParams.append("to", params.to);
    }

    if (params.limit) {
      queryParams.append("limit", Math.min(params.limit, 1000).toString());
    }

    const response = await apiClient.get<HistoricalFundingRateDto[]>(
      `/History?${queryParams.toString()}`,
    );
    return response.data;
  },
};
