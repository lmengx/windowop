import { ref } from 'vue'
import { defineStore } from 'pinia'

export const usePwdStore = defineStore('Pwd', () => {
  const HashedPwd = ref("")
  const HmacKey = ref("")

  return { HashedPwd, HmacKey }
},
  {
    persist: true
  }
)
