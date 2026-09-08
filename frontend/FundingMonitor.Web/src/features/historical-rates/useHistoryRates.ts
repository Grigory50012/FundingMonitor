import { useCallback, useEffect, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { ExchangeType, HistoricalFundingRateDto } from "../../types";
import { getHistory } from "./api/getHistory";

type HistoryParams = {
  symbol: string;
  exchanges?: ExchangeType[];
  from?: string;
  to?: string;
  limit?: number;
};

export function useHistoryRates(params?: HistoryParams) {
  const [data, setData] = useState<HistoricalFundingRateDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const exchangesKey = params?.exchanges?.join(",") ?? "";

  const load = useCallback(async () => {
    const symbol = params?.symbol;
    if (!symbol) {
      setData([]);
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const response = await getHistory({
        symbol,
        exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
        from: params?.from,
        to: params?.to,
        limit: params?.limit,
      });
      setData(response as HistoricalFundingRateDto[]);
    } catch (err: unknown) {
      const message = getApiErrorMessage(
        err,
        "Не удалось загрузить исторические данные",
      );
      console.error("Failed to load history data:", err);
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [
    params?.symbol,
    exchangesKey,
    params?.from,
    params?.to,
    params?.limit,
  ]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}
