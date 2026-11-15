import { useCallback, useEffect, useRef, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { FundingArbitrageDto } from "../../types";
import { getArbitrage, type GetArbitrageParams } from "./api/getArbitrage";

export function useArbitrageRates(params: GetArbitrageParams, enabled: boolean) {
  const [data, setData] = useState<FundingArbitrageDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const activeRequest = useRef<AbortController | null>(null);
  const { symbol, minAprPercent, maxAdversePriceSpreadPercent, sameFundingIntervalOnly } = params;
  const exchangesKey = params.exchanges?.join(",") ?? "";

  const load = useCallback(async () => {
    if (!enabled) return;
    activeRequest.current?.abort();
    const request = new AbortController();
    activeRequest.current = request;
    setIsLoading(true);
    setError(null);
    try {
      const response = await getArbitrage({
        symbol,
        exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
        minAprPercent,
        maxAdversePriceSpreadPercent,
        sameFundingIntervalOnly,
      }, request.signal);
      if (!request.signal.aborted) setData(response);
    } catch (err: unknown) {
      if (!request.signal.aborted) {
        setData([]);
        setError(getApiErrorMessage(err, "Не удалось загрузить арбитражные данные"));
      }
    } finally {
      if (!request.signal.aborted) setIsLoading(false);
    }
  }, [enabled, symbol, exchangesKey, minAprPercent, maxAdversePriceSpreadPercent, sameFundingIntervalOnly]);

  useEffect(() => {
    void load();
    return () => activeRequest.current?.abort();
  }, [load]);

  return { data, isLoading: enabled && isLoading, error, refresh: load };
}
