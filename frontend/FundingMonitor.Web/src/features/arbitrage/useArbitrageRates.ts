import { useCallback, useEffect, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { ExchangeType, FundingArbitrageDto } from "../../types";
import { getArbitrage } from "./api/getArbitrage";

type ArbitrageParams = {
  symbol?: string;
  exchanges?: ExchangeType[];
};

export function useArbitrageRates(params?: ArbitrageParams) {
  const [data, setData] = useState<FundingArbitrageDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const exchangesKey = params?.exchanges?.join(",") ?? "";

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const response = await getArbitrage({
        symbol: params?.symbol || undefined,
        exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
      });
      setData(response);
    } catch (err: unknown) {
      const message = getApiErrorMessage(
        err,
        "Не удалось загрузить арбитражные данные",
      );
      console.error("Failed to load arbitrage data:", err);
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
