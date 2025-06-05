<template>
    <h1>保护设置</h1>
    <hr />

    <div>

      <el-text>双进程保护：</el-text>
      <el-switch v-model="Protect_Process"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('Protect_Process', SettingData.Protect_Process ? 0:1)" />

      <br />
      <el-text>文件保护：</el-text>
      <el-switch v-model="Protect_Files"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('Protect_Files', SettingData.Protect_Files ? 0:1)" />

      <br />
      <el-text>启动项保护：</el-text>
      <el-switch v-model="Protect_StartTask"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('Protect_StartTask', SettingData.Protect_StartTask ? 0:1)" />

    </div>

</template>

<script setup>
  import { ref } from 'vue'

  const props = defineProps({
        SettingData: Object
    });

  const Protect_Process = ref(false)
  const Protect_Files = ref(false)
  const Protect_StartTask = ref(false)

  onMounted(() => {
    SetRef(props.SettingData)
  });

  watch(props.SettingData, (newValue) => {
    SetRef(newValue)
  })

  function SetRef(newValue) {
    Protect_Process.value = newValue.Protect_Process == 1
    Protect_Files.value = newValue.Protect_Files == 1
    Protect_StartTask.value = newValue.Protect_StartTask == 1
  }

    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);


    function Setting_ReadAll() {
        emit("Setting_Read_Batch");
    }

    function Setting_Write(item, value) {
        emit("Setting_Write", item, value);
        Setting_ReadAll();
  }

  function Change(item) {
    Setting_Write(item, !props.SettingData[item])
    return true
  }





</script>

