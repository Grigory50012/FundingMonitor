export function arbitrageRefreshOptions(autoRefresh: boolean) {
  return {
    staleTime: 5_000,
    refetchInterval: false as const,
    refetchIntervalInBackground: false,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
    retry: autoRefresh ? 2 : false as const,
    retryDelay: 2_000,
  };
}
