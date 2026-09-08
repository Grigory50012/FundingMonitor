import { apiClient } from "../../../shared/api/client";
import type { components } from "../../../types/generated/api";

type FundingArbitrageDto = components["schemas"]["FundingArbitrageDto"];

type GetArbitrageParams = {
  symbol?: string;
  exchanges?: string[];
};

export async function getArbitrage(
  params?: GetArbitrageParams,
): Promise<FundingArbitrageDto[]> {
  const queryParams = new URLSearchParams();

  if (params?.symbol) queryParams.append("symbol", params.symbol);
  if (params?.exchanges?.length) {
    queryParams.append("exchanges", params.exchanges.join(","));
  }

  const response = await apiClient.get<FundingArbitrageDto[]>(
    `/Arbitrage?${queryParams.toString()}`,
  );
  return Array.isArray(response.data) ? response.data : [];
}
