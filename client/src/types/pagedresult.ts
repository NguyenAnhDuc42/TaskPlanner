export interface PagedResult<T> {
  data: T;
  nextCursor: string | null;
  hasNextPage: boolean;
}
