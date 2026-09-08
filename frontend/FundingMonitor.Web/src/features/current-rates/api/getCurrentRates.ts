import { apiClient } from "../../../shared/api/client";
import type { components } from "../../../types/generated/api";

type FundingRateDto = components["schemas"]["FundingRateDto"];

type GetCurrentRatesParams = {
  symbol?: string;
  exchanges?: string[];
};

export async function getCurrentRates(
  params?: GetCurrentRatesParams,
): Promise<FundingRateDto[]> {
  const queryParams = new URLSearchParams();

  if (params?.symbol) queryParams.append("symbol", params.symbol);
  if (params?.exchanges?.length) {
    queryParams.append("exchanges", params.exchanges.join(","));
  }

  const response = await apiClient.get<FundingRateDto[]>(
    `/FundingRates?${queryParams.toString()}`,
  );
  return Array.isArray(response.data) ? response.data : [];
}
