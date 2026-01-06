<template>
    <h1>调试设置</h1>


    <div v-auto-animate>

      <hr />
      <h3>程序实时调控</h3>

      <br />
      <el-button type="primary" plain @click="RunActionNoPara('restart')">重启程序</el-button>
      <el-button type="primary" plain @click="RunActionNoPara('RestartAsAdmin')">以管理员权限重启程序</el-button>
      <el-button type="primary" plain @click="RunActionNoPara('exit')">退出程序</el-button>
      <hr />

      <h3>日志设置</h3>
      <el-text>启用日志 </el-text>
      <el-switch v-model="Log_Enable"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('Log_Enable', SettingData.Log_Enable ? 0:1)" />
      <br />
      <div v-if="Log_Enable">

        <el-text>在程序中输出调试信息 </el-text>
        <el-switch v-model="Log_PrintDebugMsg"
                   :before-change="() =>{return true}"
                   @click="Setting_Write('Log_PrintDebugMsg', SettingData.Log_PrintDebugMsg ? 0:1)" />
        <br />
        <el-text>记录窗口检测服务日志 </el-text>
        <el-switch v-model="Log_WindowWatcher"
                   :before-change="() =>{return true}"
                   @click="Setting_Write('Log_WindowWatcher', SettingData.Log_WindowWatcher ? 0:1)" />

        <br />
        <el-text>记录网页服务日志 </el-text>
        <el-switch v-model="Log_WebServer"
                   :before-change="() =>{return true}"
                   @click="Setting_Write('Log_WebServer', SettingData.Log_WebServer ? 0:1)" />
        <br />
        <el-text>在网页控制台输出调试信息 </el-text>
        <el-switch v-model="DataStore.DebugMode"/>

      </div>
      <hr />

      <h3>监听设置</h3>
      <h5>端口</h5>
      <el-input style="width: 240px" v-model="SettingData.ListenPort" placeholder="输入端口" />
      <el-button type="primary" plain @click="Setting_Write('ListenPort', SettingData.ListenPort)">提交</el-button>
      <br />

      <el-popover placement="right"
                  title="注意"
                  :width="200"
                  trigger="hover"
                  content="这个操作需要管理员权限">
        <template #reference>
          <el-text>
            全前缀监听服务<el-icon><IEpInfoFilled /></el-icon>
          </el-text>
        </template>
      </el-popover>

      <el-switch v-model="ServeAllPrefix"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('ServeAllPrefix', SettingData.ServeAllPrefix ? 0:1)" />
      <br />
      <el-button type="primary" plain v-if="SettingData.ServeAllPrefix == '1'" @click="visitLANAddress">局域网访问本页面</el-button>
      <br v-if="SettingData.ServeAllPrefix == '1'" />
      <el-text>以管理员权限运行 </el-text>
      <el-switch v-model="RunAsAdmin"
                 :before-change="() =>{return true}"
                 @click="Setting_Write('RunAsAdmin', SettingData.RunAsAdmin ? 0:1)" />
      <br />

      <el-popover placement="right"
                  title="这个功能的作用"
                  :width="200"
                  trigger="hover"
                  content="关闭UAC可以使程序以管理员身份运行时无需用户确认">
        <template #reference>
          <el-text>
            UAC设置<el-icon><IEpInfoFilled /></el-icon>
          </el-text>
        </template>
      </el-popover>
      <br />
      <el-button type="primary" plain @click="UAC('0')">关闭UAC</el-button>
      <el-button type="primary" plain @click="UAC('1')">恢复UAC</el-button>
      <br />
      <hr />

      <h5>退出调试模式</h5>
      <el-button type="primary" plain @click="Setting_Write('DebugMode', '0')">退出</el-button>
    </div>
</template>


<script setup>
import { ref,inject,onMounted, watch } from 'vue'
  import { useDataStore } from '../stores/dataStore.js'

    const props = defineProps({
        SettingData: Object
    });

  const Log_PrintDebugMsg = ref(false)
  const Log_WindowWatcher = ref(false)
  const Log_WebServer = ref(false)
  const Log_ShowConsole = ref(false)
  const Log_Enable = ref(false)
  const ServeAllPrefix = ref(false)
  const RunAsAdmin = ref(false)
      const DataStore = useDataStore()


  onMounted(() => {
    SetRef(props.SettingData)
  });

  watch(props.SettingData, (newValue) => {
    SetRef(newValue)
  })

  function SetRef(newValue) {
    Log_ShowConsole.value = newValue.Log_ShowConsole == 1
    Log_Enable.value = newValue.Log_Enable == 1
    ServeAllPrefix.value = newValue.ServeAllPrefix == 1
    RunAsAdmin.value = newValue.RunAsAdmin == 1

    Log_PrintDebugMsg.value = newValue.Log_PrintDebugMsg == 1
    Log_WindowWatcher.value = newValue.Log_WindowWatcher == 1
    Log_WebServer.value = newValue.Log_WebServer == 1
  }


    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);
    const RunActions = inject("provideFuncRunActions")
    const RunActionNoPara = inject("provideFuncRunActionNoPara")
    function Setting_ReadAll() {
        emit("Setting_Read_Batch");
    }
    function Setting_Write(item, value) {
        emit("Setting_Write", item, value);
        Setting_ReadAll();
  }

    function visitLANAddress()
    {
        window.location.assign(props.SettingData.LANAddress);
    }

    function UAC(op)
    {
        const ActionCalls = `[
            {
                "ActionName": "UAC",
                "Paras": [
                    {"ParaName": "op", "value": "${op}"}
                ]
            }
        ]`;
        RunActions(JSON.parse(ActionCalls));
    }

</script>

