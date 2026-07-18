import axios from "axios";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
  // Without this, a request that hangs (backend restarting mid-request, dead connection) never
  // rejects — TransactionQueue.flush()'s `flushing` reentrancy guard then never resets in its
  // `finally`, silently wedging every future flush for the rest of the page session.
  timeout: 30000,
  paramsSerializer: {
    serialize: (params) => {
      const sp = new URLSearchParams();
      for (const [key, val] of Object.entries(params)) {
        if (val === undefined || val === null) continue;
        if (Array.isArray(val)) {
          val.forEach((v) => sp.append(key, String(v)));
        } else {
          sp.append(key, String(val));
        }
      }
      return sp.toString();
    },
  },
});
