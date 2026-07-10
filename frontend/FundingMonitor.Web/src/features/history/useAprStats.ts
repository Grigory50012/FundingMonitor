import { useEffect, useState } from "react";
import { fundingRatesApi } from "../../api/fundingRates";
import { getApiErrorMessage } from "../../shared/api/errors";
import type { AprPeriodStatsDto, ExchangeType } from "../../types";

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
        const stats = await fundingRatesApi.getAprStats({
          symbol,
          exchanges:
            selectedExchanges.length > 0 ? selectedExchanges : undefined,
        });
        if (isActive) setData(stats);
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
  }, [symbol, selectedExchanges]);

  return { data, isLoading, error };
}
