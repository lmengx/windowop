import { useDataStore } from '@/stores/dataStore.js'

export function safeJsonParse(str) {
  try {
    return JSON.parse(str)
  } catch (e) {
    const DataStore = useDataStore()
    DataStore.Log(e)
    return null
  }
}
