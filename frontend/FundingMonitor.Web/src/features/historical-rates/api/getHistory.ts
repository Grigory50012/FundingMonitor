import { apiClient } from "../../../shared/api/client";
import type { components } from "../../../types/generated/api";

type HistoricalFundingRateDto = components["schemas"]["HistoricalFundingRateDto"];

type GetHistoryParams = {
  symbol: string;
  exchanges?: string[];
  from?: string;
  to?: string;
  limit?: number;
};

export async function getHistory(
  params: GetHistoryParams,
): Promise<HistoricalFundingRateDto[]> {
  const queryParams = new URLSearchParams();

  queryParams.append("symbol", params.symbol);
  if (params.exchanges?.length) {
    queryParams.append("exchanges", params.exchanges.join(","));
  }
  if (params.from) queryParams.append("from", params.from);
  if (params.to) queryParams.append("to", params.to);
  if (params.limit) {
    queryParams.append("limit", Math.min(params.limit, 1000).toString());
  }

  const response = await apiClient.get<HistoricalFundingRateDto[]>(
    `/History?${queryParams.toString()}`,
  );
  return Array.isArray(response.data) ? response.data : [];
}
