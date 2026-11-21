import { useQuery } from "@tanstack/react-query";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { FundingArbitrageDto } from "../../types";
import { getArbitrage, type GetArbitrageParams } from "./api/getArbitrage";
import { arbitrageRefreshOptions } from "./arbitrageRefreshOptions";

const EMPTY_ARBITRAGE: FundingArbitrageDto[] = [];

export function useArbitrageRates(params: GetArbitrageParams, enabled: boolean, autoRefresh = true) {
  const filters = { ...params, exchanges: [...(params.exchanges ?? [])].sort() };
  const query = useQuery({
    queryKey: ["arbitrage", filters],
    queryFn: ({ signal }) => getArbitrage(filters, signal),
    enabled,
    ...arbitrageRefreshOptions(autoRefresh),
  });

  return {
    data: (query.data as FundingArbitrageDto[] | undefined) ?? EMPTY_ARBITRAGE,
    isLoading: enabled && query.isLoading,
    error: query.error ? getApiErrorMessage(query.error, "Не удалось обновить арбитражные данные") : null,
    filtersKey: JSON.stringify(filters),
    refresh: query.refetch,
  };
}
