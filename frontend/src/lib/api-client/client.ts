import axios from "axios";

export const api = axios.create({
  baseURL: "/api",
  withCredentials: true,
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
