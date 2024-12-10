import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useSettingDataStore = defineStore('SettingData', () => {
  const data = ref(
    {
      "SleepTime":1000,
      "ListenPort":7799,
      "ServeAllPrefix":0,
      "Log_Enable":0,
      "Log_PrintDebugMsg":0,
      "Log_ShowConsole":0,
      "Log_WebServer":0,
      "Log_WindowWatcher":0,
      "RunAsAdmin":0,
      "Protect_Process":0,
      "Protect_Files":0,
      "Protect_StartTask":0,
      "DebugMode":0,
      "LANAddress":"http://10.0.13.55:7799",
      "LatestVersion":"0.0.0.1"
    })

  return { data }
})
