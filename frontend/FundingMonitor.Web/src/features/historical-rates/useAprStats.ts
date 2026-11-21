import { useQuery } from "@tanstack/react-query";
import { getApiErrorMessage } from "../../shared/api/errors";
import { toUsdtSymbol } from "../../shared/lib/symbols";
import type { AprPeriodStatsDto, ExchangeType } from "../../types";
import { getAprStats } from "./api/getAprStats";
import { historyRefreshOptions } from "./historyRefreshSchedule";

type UseAprStatsParams = {
  symbol: string;
  selectedExchanges: ExchangeType[];
  enabled?: boolean;
};

const EMPTY_STATS: AprPeriodStatsDto[] = [];

export function useAprStats({ symbol, selectedExchanges, enabled = true }: UseAprStatsParams) {
  const normalizedSymbol = toUsdtSymbol(symbol);
  const exchanges = [...selectedExchanges].sort();
  const query = useQuery({
    queryKey: ["historical-rates", normalizedSymbol, "apr", { exchanges }],
    queryFn: ({ signal }) => getAprStats({
      symbol: normalizedSymbol,
      exchanges: exchanges.length ? exchanges : undefined,
    }, signal),
    enabled: enabled && Boolean(normalizedSymbol),
    ...historyRefreshOptions,
  });

  return {
    data: (query.data as AprPeriodStatsDto[] | undefined) ?? EMPTY_STATS,
    isLoading: query.isLoading,
    hasData: query.data !== undefined,
    error: query.error
      ? getApiErrorMessage(query.error, "Ошибка загрузки данных")
      : null,
  };
}
