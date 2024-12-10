<template>
    <h1>设置</h1>
    <div>
        <button class="btn" @click="() => showPage = 'Setting_Common'">常规设置</button>
        <button class="btn" @click="() => showPage = 'Setting_Protect'">保护设置</button>
        <button class="btn" @click="() => showPage = 'Setting_Update'">版本更新</button>
        <button class="btn" @click="() => showPage = 'Setting_About'">关于本软件</button>
        <button class="btn" v-show="SettingData.DebugMode == 1" @click="() => showPage = 'Setting_Debug'">调试设置</button>

    </div>

    <Setting_Common v-if="showPage == 'Setting_Common'"
                    @Setting_Write="Setting_Write"
                    @Setting_Read_Batch="Setting_Read_Batch"
                    :SettingData="SettingData" />

    <Setting_Protect v-if="showPage == 'Setting_Protect'"
                     @Setting_Write="Setting_Write"
                     @Setting_Read_Batch="Setting_Read_Batch"
                     :SettingData="SettingData" />

    <Setting_Update v-if="showPage == 'Setting_Update'"
                     :SettingData="SettingData" />

    <Setting_About v-if="showPage == 'Setting_About'"
                     @Setting_Write="Setting_Write"
                     @Setting_Read_Batch="Setting_Read_Batch"
                   />

    <Setting_Debug v-if="showPage == 'Setting_Debug'"
                   @Setting_Write="Setting_Write"
                   @Setting_Read_Batch="Setting_Read_Batch"
                   :SettingData="SettingData" />
</template>

<script setup>
    import { ref, watch, onMounted, inject } from 'vue';
    import Setting_Common from './Setting_Common.vue';
    import Setting_Protect from './Setting_Protect.vue';
    import Setting_Debug from './Setting_Debug.vue';
    import Setting_About from './Setting_About.vue';
    import Setting_Update from './Setting_Update.vue';

    const showPage = ref('Setting_Common');

    const SendMsg = inject("provideFuncSendWSMsg")
    const receivedEvent = inject("provideReceivedMsg")

    onMounted(() => {
        Setting_Read_Batch()
    });

    watch(receivedEvent, (newValue) => {
                receivedMessage(newValue.text)
            })

    const SettingData = ref({});

    function receivedMessage(eventData) {
        if (eventData.startsWith("SettingQueryResult:"))
        {
            const jsonString = eventData.slice("SettingQueryResult:".length);
            const jsonData = JSON.parse(jsonString);

            SettingData.value = jsonData;

        }

    }

    function Setting_Read_Batch()
    {
        const reqJson = `{"Operation": "Setting_Read_Batch"}`;
        SendMsg(reqJson)
    }

    const Items_ReadToMomory = ['Log_Enable', 'Log_PrintDebugMsg', 'Log_WebServer', 'Log_WindowWatcher', 'SleepTime', 'Protect_Process', 'Protect_StartTask', 'Protect_Files'];

    function Setting_Write(item, value)
    {
        var reqJson = `{"Operation": "Setting_Write",
                            "Item": "${item}",
                            "Value": "${value}"
                        }`;
                        SendMsg(reqJson)
        if (!Items_ReadToMomory.includes(item)) return;
        //只对部分目标实时读取
        reqJson = `{"Operation": "ReadToMemory",
                            "Item": "${item}"
                        }`;
                        SendMsg(reqJson)
    }


</script>

