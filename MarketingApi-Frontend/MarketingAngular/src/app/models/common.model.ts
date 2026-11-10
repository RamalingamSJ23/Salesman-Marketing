export enum RecordStatus {
  Active = 1,
  Inactive = 2,
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface SelectOption {
  value: number;
  name: string;
  displayText: string;
}
