import { useCallback, useEffect, useState } from "react";
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
      const response = await getCurrentRates({
        symbol,
        exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
      });
      setData(response as FundingRateDto[]);
    } catch (err: unknown) {
      const message = getApiErrorMessage(
        err,
        "Не удалось загрузить текущие данные",
      );
      console.error("Failed to load current data:", err);
      setError(message);
    } finally {
      setIsLoading(false);
    }
  }, [params?.symbol, exchangesKey]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}
