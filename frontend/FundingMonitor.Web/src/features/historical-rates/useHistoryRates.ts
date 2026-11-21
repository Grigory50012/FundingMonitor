import { useQuery, useQueryClient } from "@tanstack/react-query";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { ExchangeType, HistoricalFundingRateDto } from "../../types";
import { getHistory } from "./api/getHistory";
import { historyRefreshOptions } from "./historyRefreshSchedule";

type HistoryParams = {
  symbol: string;
  exchanges?: ExchangeType[];
  from?: string;
  to?: string;
  limit?: number;
};

const EMPTY_HISTORY: HistoricalFundingRateDto[] = [];

export function useHistoryRates(params?: HistoryParams) {
  const queryClient = useQueryClient();
  const { symbol = "", from, to, limit } = params ?? {};
  const exchanges = [...(params?.exchanges ?? [])].sort();
  const query = useQuery({
    queryKey: ["historical-rates", symbol, "history", { exchanges, from, to, limit }],
    queryFn: ({ signal }) => getHistory({
      symbol, exchanges: exchanges.length ? exchanges : undefined, from, to, limit,
    }, signal),
    enabled: Boolean(symbol),
    ...historyRefreshOptions,
  });

  return {
    data: (query.data as HistoricalFundingRateDto[] | undefined) ?? EMPTY_HISTORY,
    isLoading: query.isLoading,
    error: query.error
      ? getApiErrorMessage(query.error, "Не удалось загрузить исторические данные")
      : null,
    refresh: () => queryClient.invalidateQueries({ queryKey: ["historical-rates", symbol] }),
  };
}
