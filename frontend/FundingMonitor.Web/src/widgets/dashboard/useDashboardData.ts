import { useCallback, useEffect, useMemo, useState } from "react";
import { STORAGE_KEYS } from "../../config/storageKeys";
import { useArbitrageRates } from "../../features/arbitrage/useArbitrageRates";
import { useCurrentRates } from "../../features/current-rates/useCurrentRates";
import { getCurrentRates } from "../../features/current-rates/api/getCurrentRates";
import { useHistoryRates } from "../../features/historical-rates/useHistoryRates";
import { useAprStats } from "../../features/historical-rates/useAprStats";
import { getFundingExchangeOrder } from "./fundingExchangeOrder";
import type { FundingExchangeSort } from "./fundingExchangeOrder";
import { useLocalStorage } from "../../shared/hooks/useLocalStorage";
import { toUsdtSymbol } from "../../shared/lib/symbols";
import type { ExchangeType } from "../../types";
import type { TimeRangeType } from "../../types/history";
import { startDashboardRefresh } from "./dashboardRefresh";

const DEFAULT_COINS = ["BTC", "ETH", "SOL", "XRP", "DOGE"];

export type HistoryViewMode = "chart" | "table";

export type FilterState = {
  exchanges: ExchangeType[];
  symbol: string;
};

export const EMPTY_ARBITRAGE_FILTERS = {
  exchanges: [] as ExchangeType[],
  symbol: "",
  minAprPercent: "",
  maxAdversePriceSpreadPercent: "",
  sameFundingIntervalOnly: false,
};

export function parsePercent(value: string): number | undefined {
  const trimmed = value.trim();
  if (!/^(\d+([.,]\d*)?|[.,]\d+)$/.test(trimmed)) return undefined;
  const parsed = Number(trimmed.replace(",", "."));
  return Number.isFinite(parsed) ? parsed : undefined;
}

export function useDashboardData() {
  const [mainFilters, setMainFilters] = useLocalStorage<FilterState>(
    STORAGE_KEYS.mainFilters,
    {
      exchanges: [],
      symbol: "BTC",
    },
  );
  const [arbitrageFilters, setArbitrageFilters] = useState(EMPTY_ARBITRAGE_FILTERS);
  const [arbitrageAutoRefresh, setArbitrageAutoRefresh] = useState(true);
  const [historyViewMode, setHistoryViewMode] = useState<HistoryViewMode>("chart");
  const [exchangeSort, setExchangeSort] = useState<FundingExchangeSort>({
    source: "current", config: { column: "fundingRate", direction: null },
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

  const aprHistory = useAprStats({
    symbol: mainFilters.symbol,
    selectedExchanges: mainFilters.exchanges,
    enabled: historyViewMode === "table",
  });
  const exchangeOrder = useMemo(() => getFundingExchangeOrder(
    current.data, aprHistory.data, mainFilters.exchanges, exchangeSort,
  ), [current.data, aprHistory.data, mainFilters.exchanges, exchangeSort]);

  const minAprPercent = parsePercent(arbitrageFilters.minAprPercent);
  const maxAdversePriceSpreadPercent = parsePercent(arbitrageFilters.maxAdversePriceSpreadPercent);
  const arbitrageFiltersValid =
    (!arbitrageFilters.minAprPercent.trim() || minAprPercent !== undefined) &&
    (!arbitrageFilters.maxAdversePriceSpreadPercent.trim() || maxAdversePriceSpreadPercent !== undefined);

  const arbitrage = useArbitrageRates({
    symbol: arbitrageFilters.symbol || undefined,
    minAprPercent,
    maxAdversePriceSpreadPercent,
    sameFundingIntervalOnly: arbitrageFilters.sameFundingIntervalOnly,
    exchanges:
      arbitrageFilters.exchanges.length > 0
        ? arbitrageFilters.exchanges
        : undefined,
  }, arbitrageFiltersValid, arbitrageAutoRefresh);

  const refreshCurrent = current.refresh;
  const refreshArbitrage = arbitrage.refresh;
  useEffect(() => startDashboardRefresh(() => {
    void refreshCurrent(true);
    if (arbitrageAutoRefresh && arbitrageFiltersValid) {
      void refreshArbitrage({ cancelRefetch: false });
    }
  }), [refreshCurrent, refreshArbitrage, arbitrageAutoRefresh, arbitrageFiltersValid]);

  const [allCoins, setAllCoins] = useState<string[]>([]);
  const [timeRange, setTimeRange] = useState<TimeRangeType>("1d");

  const loadAllCoins = useCallback(async () => {
    try {
      const data = await getCurrentRates({});
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
    arbitrageFiltersValid,
    arbitrageAutoRefresh,
    setArbitrageAutoRefresh,
    current,
    history,
    aprHistory,
    exchangeOrder,
    exchangeSort,
    setExchangeSort,
    arbitrage,
    availableCoins,
    historyViewMode,
    setHistoryViewMode,
    timeRange,
    setTimeRange,
    errorMessage: current.error ?? history.error ?? arbitrage.error ?? null,
  };
}
