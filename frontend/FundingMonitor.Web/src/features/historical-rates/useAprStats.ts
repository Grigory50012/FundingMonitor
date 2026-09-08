import { useEffect, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { AprPeriodStatsDto, ExchangeType } from "../../types";
import { getAprStats } from "./api/getAprStats";

type UseAprStatsParams = {
  symbol: string;
  selectedExchanges: ExchangeType[];
};

export function useAprStats({
  symbol,
  selectedExchanges,
}: UseAprStatsParams) {
  const [data, setData] = useState<AprPeriodStatsDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const exchangesKey = selectedExchanges.join(",");

  useEffect(() => {
    if (!symbol) {
      setData([]);
      return;
    }

    let isActive = true;

    const loadData = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const stats = await getAprStats({
          symbol,
          exchanges: exchangesKey ? exchangesKey.split(",") : undefined,
        });
        if (isActive) setData(stats as AprPeriodStatsDto[]);
      } catch (err) {
        if (isActive) {
          setError(
            getApiErrorMessage(
              err,
              "Ошибка загрузки данных",
            ),
          );
        }
      } finally {
        if (isActive) setIsLoading(false);
      }
    };

    loadData();

    return () => {
      isActive = false;
    };
  }, [symbol, exchangesKey]);

  return { data, isLoading, error };
}
