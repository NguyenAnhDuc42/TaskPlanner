export const API_BASE = process.env.API_URL
if (!API_BASE) {
  throw new Error('Missing API_URL')
}
