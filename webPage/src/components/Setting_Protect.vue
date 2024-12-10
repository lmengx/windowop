<template>
    <h1>保护设置</h1>
    <hr />

    <div>

        <p>双进程保护：{{SettingData.Protect_Process == '0' ? '关':'开'}}</p>
        <button class="btn" v-show="SettingData.Protect_Process == '0'" @click="Setting_Write('Protect_Process', '1')">开启</button>
        <button class="btn" v-show="SettingData.Protect_Process == '1'" @click="Setting_Write('Protect_Process', '0')">关闭</button>

        <p>文件保护：{{SettingData.Protect_Files == '0' ? '关':'开'}}</p>
        <button class="btn" v-show="SettingData.Protect_Files == '0'" @click="Setting_Write('Protect_Files', '1')">开启</button>
        <button class="btn" v-show="SettingData.Protect_Files == '1'" @click="Setting_Write('Protect_Files', '0')">关闭</button>

        <p>启动项保护：{{SettingData.Protect_StartTask == '0' ? '关':'开'}}</p>
        <button class="btn" v-show="SettingData.Protect_StartTask == '0'" @click="Setting_Write('Protect_StartTask', '1')">开启</button>
        <button class="btn" v-show="SettingData.Protect_StartTask == '1'" @click="Setting_Write('Protect_StartTask', '0')">关闭</button>
        <hr />
    </div>

</template>

<script setup>
  import { ref } from 'vue'

  const props = defineProps({
        SettingData: Object
    });

    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);


    function Setting_ReadAll() {
        emit("Setting_Read_Batch");
    }

    function Setting_Write(item, value) {
        emit("Setting_Write", item, value);
        Setting_ReadAll();
    }





</script>

