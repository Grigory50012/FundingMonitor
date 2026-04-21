import { useCallback, useEffect, useState } from "react";
import { fundingRatesApi } from "../api/fundingRates";
import type {
  FundingRateDto,
  HistoricalFundingRateDto,
  FundingArbitrageDto,
  ExchangeType,
} from "../types";

type CurrentParams = {
  symbol?: string;
  exchanges?: ExchangeType[];
  includeInactive?: boolean;
};

export function useCurrentRates(params?: CurrentParams) {
  const [data, setData] = useState<FundingRateDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    const symbol = params?.symbol;
    if (!symbol) {
      setData([]);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      const res = await fundingRatesApi.getCurrentRates({
        symbol,
        exchanges: params?.exchanges?.length ? params.exchanges : undefined,
        includeInactive: params?.includeInactive,
      });
      setData(res);
    } catch (err: any) {
      const msg =
        err?.response?.data?.details ||
        err?.message ||
        "Не удалось загрузить текущие данные";
      console.error("Failed to load current data:", err);
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [params?.symbol, params?.exchanges, params?.includeInactive]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}

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

  const load = useCallback(async () => {
    const symbol = params?.symbol;
    if (!symbol) {
      setData([]);
      return;
    }
    setIsLoading(true);
    setError(null);
    try {
      const res = await fundingRatesApi.getHistory({
        symbol,
        exchanges: params?.exchanges?.length ? params.exchanges : undefined,
        from: params?.from,
        to: params?.to,
        limit: params?.limit,
      });
      setData(res);
    } catch (err: any) {
      const msg =
        err?.response?.data?.details ||
        err?.message ||
        "Не удалось загрузить исторические данные";
      console.error("Failed to load history data:", err);
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [params?.symbol, params?.exchanges, params?.from, params?.to, params?.limit]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}

type ArbitrageParams = {
  symbol?: string;
  exchanges?: ExchangeType[];
};

export function useArbitrageRates(params?: ArbitrageParams) {
  const [data, setData] = useState<FundingArbitrageDto[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      const res = await fundingRatesApi.getArbitrageOpportunities({
        symbol: params?.symbol || undefined,
        exchanges: params?.exchanges?.length ? params.exchanges : undefined,
      });
      setData(res);
    } catch (err: any) {
      const msg =
        err?.response?.data?.details ||
        err?.message ||
        "Не удалось загрузить арбитражные данные";
      console.error("Failed to load arbitrage data:", err);
      setError(msg);
    } finally {
      setIsLoading(false);
    }
  }, [params?.symbol, params?.exchanges]);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}
