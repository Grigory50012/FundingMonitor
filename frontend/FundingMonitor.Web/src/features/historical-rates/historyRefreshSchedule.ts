const HOUR_MS = 60 * 60 * 1000;
const COLLECTION_DELAY_MS = 60 * 1000;

export function getNextHistoryRefreshAt(now: number): number {
  const hourStart = Math.floor(now / HOUR_MS) * HOUR_MS;
  const refreshAt = hourStart + COLLECTION_DELAY_MS;
  return refreshAt > now ? refreshAt : refreshAt + HOUR_MS;
}

export const historyRefreshOptions = {
  staleTime: (query: { state: { dataUpdatedAt: number } }) =>
    getNextHistoryRefreshAt(query.state.dataUpdatedAt) - query.state.dataUpdatedAt,
  refetchInterval: () => {
    const now = Date.now();
    return getNextHistoryRefreshAt(now) - now;
  },
  refetchIntervalInBackground: false,
  retry: 3,
  retryDelay: (attempt: number) => Math.min(2_000 * 2 ** attempt, 30_000),
};
