import { apiClient } from "../../../shared/api/client";
import type { components } from "../../../types/generated/api";

type AprPeriodStatsDto = components["schemas"]["AprPeriodStatsDto"];

type GetAprStatsParams = {
  symbol: string;
  exchanges?: string[];
  periods?: number[];
};

export async function getAprStats(
  params: GetAprStatsParams,
): Promise<AprPeriodStatsDto[]> {
  const queryParams = new URLSearchParams();

  queryParams.append("symbol", params.symbol);
  if (params.exchanges?.length) {
    queryParams.append("exchanges", params.exchanges.join(","));
  }
  if (params.periods?.length) {
    queryParams.append("periods", params.periods.join(","));
  }

  const response = await apiClient.get<AprPeriodStatsDto[]>(
    `/History/apr-stats?${queryParams.toString()}`,
  );
  return Array.isArray(response.data) ? response.data : [];
}
