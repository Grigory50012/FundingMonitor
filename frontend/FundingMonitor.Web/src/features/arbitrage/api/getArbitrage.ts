import { apiClient } from "../../../shared/api/client";
import type { components } from "../../../types/generated/api";

type FundingArbitrageDto = components["schemas"]["FundingArbitrageDto"];

export type GetArbitrageParams = {
  symbol?: string;
  exchanges?: string[];
  minAprPercent?: number;
  maxAdversePriceSpreadPercent?: number;
  sameFundingIntervalOnly?: boolean;
};

export async function getArbitrage(
  params?: GetArbitrageParams,
  signal?: AbortSignal,
): Promise<FundingArbitrageDto[]> {
  const queryParams = new URLSearchParams();

  if (params?.symbol) queryParams.append("symbol", params.symbol);
  if (params?.exchanges?.length) {
    queryParams.append("exchanges", params.exchanges.join(","));
  }

  if (params?.minAprPercent !== undefined)
    queryParams.set("minAprPercent", String(params.minAprPercent));
  if (params?.maxAdversePriceSpreadPercent !== undefined)
    queryParams.set("maxAdversePriceSpreadPercent", String(params.maxAdversePriceSpreadPercent));
  if (params?.sameFundingIntervalOnly)
    queryParams.set("sameFundingIntervalOnly", "true");

  const query = queryParams.toString();
  const response = await apiClient.get<FundingArbitrageDto[]>(
    query ? `/Arbitrage?${query}` : "/Arbitrage",
    { signal },
  );
  return Array.isArray(response.data) ? response.data : [];
}
