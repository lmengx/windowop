import { ref } from 'vue'
import { defineStore } from 'pinia'

export const usePwdStore = defineStore('Pwd', () => {
  const HashedPwd = ref("")
  const HmacKey = ref("")

  const DebugMode = ref(false)

  const ConnectTargets = ref([
    { name: "默认目标", address: "[visitTarget]" },
    { name: "本地测试", address: "ws://127.0.0.1:7799" }
  ])

  const DefTarget = ref("默认目标")

  function addTarget(name, address) {
      if (ConnectTargets.value.some(target => target.name === name)) {
        throw new Error(`目标名称 ${name} 已存在`)
      }
      ConnectTargets.value.push({ name, address })
    }

    function removeTarget(name) {
      const index = ConnectTargets.value.findIndex(target => target.name === name)
      if (index === -1) {
        throw new Error(`目标名称 ${name} 不存在`)
      }
      ConnectTargets.value.splice(index, 1)

      if (DefTarget.value === name) {
        DefTarget.value = ConnectTargets.value[0]?.name || ""
      }
    }

    function updateTarget(oldName, newName, newAddress) {
      const target = ConnectTargets.value.find(target => target.name === oldName)
      if (!target) {
        throw new Error(`目标名称 ${oldName} 不存在`)
      }

      if (newName !== oldName && ConnectTargets.value.some(t => t.name === newName)) {
        throw new Error(`目标名称 ${newName} 已存在`)
      }

      target.name = newName
      target.address = newAddress

      if (DefTarget.value === oldName) {
        DefTarget.value = newName
      }
    }

    function getTargets() {
      return ConnectTargets.value
    }

    function getDefaultTargetAddress() {
      const target = ConnectTargets.value.find(t => t.name === DefTarget.value)
      return target?.address || ""
    }

    function setDefaultTarget(name) {
      if (!ConnectTargets.value.some(target => target.name === name)) {
        throw new Error(`目标名称 ${name} 不存在`)
      }
      DefTarget.value = name
    }

    return {
      HashedPwd,
      HmacKey,
	  DebugMode,
      ConnectTargets,
      DefTarget,
      addTarget,
      removeTarget,
      updateTarget,
      getTargets,
      getDefaultTargetAddress,
      setDefaultTarget
    }
  }, {
    persist: true
  })
