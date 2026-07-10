import { useCallback, useEffect, useMemo, useState } from "react";
import { fundingRatesApi } from "../../api/fundingRates";
import { STORAGE_KEYS } from "../../config/storageKeys";
import {
  useArbitrageRates,
  useCurrentRates,
  useHistoryRates,
} from "../../hooks/useFundingRates";
import { useLocalStorage } from "../../hooks/useLocalStorage";
import { toUsdtSymbol } from "../../shared/lib/symbols";
import type { ExchangeType } from "../../types";
import type { TimeRangeType } from "../../types/history";

const DEFAULT_COINS = ["BTC", "ETH", "SOL", "XRP", "DOGE"];

export type HistoryViewMode = "chart" | "table";

export type FilterState = {
  exchanges: ExchangeType[];
  symbol: string;
};

export function useDashboardData() {
  const [mainFilters, setMainFilters] = useLocalStorage<FilterState>(
    STORAGE_KEYS.mainFilters,
    {
      exchanges: [],
      symbol: "BTC",
    },
  );
  const [arbitrageFilters, setArbitrageFilters] =
    useLocalStorage<FilterState>(STORAGE_KEYS.arbitrageFilters, {
      exchanges: [],
      symbol: "",
    });

  const current = useCurrentRates({
    symbol: mainFilters.symbol,
    exchanges:
      mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
  });

  const history = useHistoryRates({
    symbol: toUsdtSymbol(mainFilters.symbol),
    exchanges:
      mainFilters.exchanges.length > 0 ? mainFilters.exchanges : undefined,
    limit: 1000,
  });

  const arbitrage = useArbitrageRates({
    symbol: arbitrageFilters.symbol || undefined,
    exchanges:
      arbitrageFilters.exchanges.length > 0
        ? arbitrageFilters.exchanges
        : undefined,
  });

  const [allCoins, setAllCoins] = useState<string[]>([]);
  const [historyViewMode, setHistoryViewMode] =
    useState<HistoryViewMode>("chart");
  const [timeRange, setTimeRange] = useState<TimeRangeType>("1d");

  const loadAllCoins = useCallback(async () => {
    try {
      const data = await fundingRatesApi.getCurrentRates({});
      const coins = Array.from(
        new Set(data.map((item) => item.symbol.replace("-USDT", ""))),
      ).sort();
      setAllCoins(coins);
    } catch (err) {
      console.error("Failed to load all coins:", err);
    }
  }, []);

  useEffect(() => {
    loadAllCoins();
  }, [loadAllCoins]);

  const availableCoins = useMemo(
    () => Array.from(new Set([...DEFAULT_COINS, ...allCoins])).sort(),
    [allCoins],
  );

  return {
    mainFilters,
    setMainFilters,
    arbitrageFilters,
    setArbitrageFilters,
    current,
    history,
    arbitrage,
    availableCoins,
    historyViewMode,
    setHistoryViewMode,
    timeRange,
    setTimeRange,
    errorMessage: current.error ?? history.error ?? arbitrage.error ?? null,
  };
}
