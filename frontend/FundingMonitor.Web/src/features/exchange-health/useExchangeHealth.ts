import { useCallback, useEffect, useState } from "react";
import { getApiErrorMessage } from "../../shared/api/errors";
import { getExchangeHealth } from "./api/getExchangeHealth";

export function useExchangeHealth() {
  const [data, setData] = useState<Record<string, boolean>>({});
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setIsLoading(true);
    setError(null);
    try {
      setData(await getExchangeHealth());
    } catch (err: unknown) {
      setError(getApiErrorMessage(err, "Не удалось загрузить статус бирж"));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return { data, isLoading, error, refresh: load };
}
