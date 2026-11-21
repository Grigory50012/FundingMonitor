export function startDashboardRefresh(refresh: () => void): () => void {
  const refreshVisible = () => {
    if (document.visibilityState === "visible") refresh();
  };
  const interval = globalThis.setInterval(refreshVisible, 5_000);
  document.addEventListener("visibilitychange", refreshVisible);
  window.addEventListener("online", refreshVisible);

  return () => {
    globalThis.clearInterval(interval);
    document.removeEventListener("visibilitychange", refreshVisible);
    window.removeEventListener("online", refreshVisible);
  };
}
