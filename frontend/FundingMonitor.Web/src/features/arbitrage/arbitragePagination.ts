export function paginateGroups<T>(groups: readonly T[], requestedPage: number, pageSize: number) {
  const pageCount = Math.max(1, Math.ceil(groups.length / pageSize));
  const page = Math.min(Math.max(1, requestedPage), pageCount);
  const offset = (page - 1) * pageSize;
  return {
    items: groups.slice(offset, offset + pageSize),
    page,
    pageCount,
    start: groups.length ? offset + 1 : 0,
    end: Math.min(offset + pageSize, groups.length),
  };
}
