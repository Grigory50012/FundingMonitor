export type SortDirection = "asc" | "desc" | null;

export function nextSortDirection(direction: SortDirection): SortDirection {
  if (direction === "asc") return "desc";
  if (direction === "desc") return null;
  return "asc";
}

export function compareNullableNumbers(
  a: number | null,
  b: number | null,
  direction: Exclude<SortDirection, null>,
): number {
  if (a === null && b === null) return 0;
  if (a === null) return 1;
  if (b === null) return -1;

  const multiplier = direction === "asc" ? 1 : -1;
  return (a - b) * multiplier;
}
