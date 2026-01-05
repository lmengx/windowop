import { ref } from 'vue'
import { defineStore } from 'pinia'

export const useDataStore = defineStore('Pwd', () => {
  const DebugMode = ref(false)

  function Log(text)
  {
    if(DebugMode.value) console.log(text)
  }

  const ConnectTargets = ref([
    { name: "默认目标", address: "[visitTarget]"},
    { name: "本地电脑", address: "ws://127.0.0.1:7799", HashedPwd: "", HmacKey: "" }
  ])

  const DefTarget = ref("默认目标")

  function addTarget(name, address)
  {
      if (ConnectTargets.value.some(target => target.name === name))
        {
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

    function getDefaultTarget() {
      const target = ConnectTargets.value.find(t => t.name === DefTarget.value)
      return target
    }



    function setDefaultTarget(name) {
      if (!ConnectTargets.value.some(target => target.name === name)) {
        throw new Error(`目标名称 ${name} 不存在`)
      }
      DefTarget.value = name
    }

function SavePwd(name,HashedPwd,HmacKey)
{

  const targetName = name ?? DefTarget.value  // 如果未提供 name，则使用默认目标名称
  const target = ConnectTargets.value.find(t => t.name === targetName) // 查找目标
  if (!target) {
    throw new Error(`目标名称 "${targetName}" 不存在`)
  }


  target.HashedPwd = HashedPwd
  target.HmacKey = HmacKey

  // 可选：记录日志（如果开启 DebugMode）
  Log(`${ HashedPwd==''?"清除":"存储" }目标 "${targetName}" 密码`)
}


    return {
	    DebugMode,
      ConnectTargets,
      DefTarget,
      addTarget,
      removeTarget,
      updateTarget,
      getDefaultTarget,
      setDefaultTarget,
      SavePwd,
      Log
    }
  }, {
    persist: true
  })
