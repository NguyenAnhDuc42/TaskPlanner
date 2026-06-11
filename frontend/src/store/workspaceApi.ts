import { createApi } from '@reduxjs/toolkit/query/react';
import type { BaseQueryFn } from '@reduxjs/toolkit/query/react';
import { api } from '@/lib/api-client';
import type { AxiosRequestConfig, AxiosError } from 'axios';

// Custom baseQuery using the existing Axios instance with important interceptors/middlewares
export const axiosBaseQuery = (): BaseQueryFn<
  {
    url: string;
    method: AxiosRequestConfig['method'];
    data?: AxiosRequestConfig['data'];
    params?: AxiosRequestConfig['params'];
  },
  unknown,
  unknown
> =>
  async ({ url, method, data, params }) => {
    try {
      const result = await api({ url, method, data, params });
      return { data: result.data };
    } catch (axiosError) {
      const err = axiosError as AxiosError;
      return {
        error: {
          status: err.response?.status,
          data: err.response?.data || err.message,
        },
      };
    }
  };

export const workspaceApi = createApi({
  reducerPath: 'workspaceApi',
  baseQuery: axiosBaseQuery(),
  tagTypes: ['Spaces', 'Folders', 'Tasks', 'Members', 'User', 'UserPreference', 'EntityAccess', 'Workflows', 'Comments', 'Documents'],
  endpoints: () => ({}), // Endpoints will be injected by features
});
