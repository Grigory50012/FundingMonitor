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
              "\u041e\u0448\u0438\u0431\u043a\u0430 \u0437\u0430\u0433\u0440\u0443\u0437\u043a\u0438 \u0434\u0430\u043d\u043d\u044b\u0445",
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
