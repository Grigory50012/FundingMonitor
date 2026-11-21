import { useCallback, useEffect, useRef, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { ExchangeType, FundingRateDto } from "../../types";
import { getCurrentRates } from "./api/getCurrentRates";

type CurrentParams = {
  symbol?: string;
  exchanges?: ExchangeType[];
};

export function useCurrentRates(params?: CurrentParams) {
  const [data, setData] = useState<FundingRateDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const activeRequest = useRef<AbortController | null>(null);
  const exchangesKey = params?.exchanges?.join(",") ?? "";

  const load = useCallback(async (background = false) => {
    if (background && activeRequest.current) return;
    activeRequest.current?.abort();
    activeRequest.current = null;
    const symbol = params?.symbol;
    if (!symbol) {
      setData([]);
      setIsLoading(false);
      setError(null);
      return;
    }

    const request = new AbortController();
    activeRequest.current = request;
    if (!background) {
      setIsLoading(true);
      setError(null);
    }
    try {
      const response = await getCurrentRates({
        symbol,
        exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
      }, request.signal);
      if (!request.signal.aborted) {
        setData(response as FundingRateDto[]);
        setError(null);
      }
    } catch (err: unknown) {
      if (request.signal.aborted) return;
      const message = getApiErrorMessage(
        err,
        "Не удалось загрузить текущие данные",
      );
      console.error("Failed to load current data:", err);
      setError(message);
    } finally {
      if (!request.signal.aborted) setIsLoading(false);
      if (activeRequest.current === request) activeRequest.current = null;
    }
  }, [params?.symbol, exchangesKey]);

  useEffect(() => {
    void load();
    return () => {
      activeRequest.current?.abort();
      activeRequest.current = null;
    };
  }, [load]);

  return { data, isLoading, error, refresh: load };
}
