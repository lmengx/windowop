<template>
    <h1>调试设置</h1>


    <div>

      <hr />
      <h3>程序实时调控</h3>
      <button class="btn" @click="RunActionNoPara('ShowConsole')">显示</button>
      <button class="btn" @click="RunActionNoPara('HideConsole')">隐藏</button>
      <br /><br />
      <button class="btn" @click="RunActionNoPara('restart')">重启程序</button>
      <button class="btn" @click="RunActionNoPara('RestartAsAdmin')">以管理员权限重启程序</button>
      <button class="btn" @click="RunActionNoPara('exit')">退出程序</button>
      <hr />

      <h3>日志设置</h3>

      <h5>启动时显示控制台：{{SettingData.Log_ShowConsole == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('Log_ShowConsole', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('Log_ShowConsole', '1')">打开</button>

      <h5>启用日志：{{SettingData.Log_Enable == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('Log_Enable', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('Log_Enable', '1')">打开</button>

      <h5>在控制台中输出调试信息：{{SettingData.Log_PrintDebugMsg == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('Log_PrintDebugMsg', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('Log_PrintDebugMsg', '1')">打开</button>

      <h5>记录窗口检测服务日志：{{SettingData.Log_WindowWatcher == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('Log_WindowWatcher', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('Log_WindowWatcher', '1')">打开</button>

      <h5>记录网页服务日志：{{SettingData.Log_WebServer == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('Log_WebServer', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('Log_WebServer', '1')">打开</button>

      <h5>在控制台输出调试信息的网页</h5>
      <button class="btn" @click="visitDebugAddress">进入</button>
      <hr />

      <h3>监听设置</h3>
      <h5>端口设置</h5>
      <input class="form-control" v-model="SettingData.ListenPort" placeholder="输入端口" />
      <button class="btn" @click="Setting_Write('ListenPort', SettingData.ListenPort)">提交</button>

      <h5>全前缀监听服务：{{SettingData.ServeAllPrefix == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('ServeAllPrefix', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('ServeAllPrefix', '1')">开启</button>

      <button class="btn" v-show="SettingData.ServeAllPrefix == '1'" @click="visitLANAddress">局域网访问本页面</button>

      <h5>UAC设置</h5>
      <button class="btn" @click="UAC('0')">关闭UAC</button>
      <button class="btn" @click="UAC('1')">恢复UAC</button>

      <h5>以管理员权限运行：{{SettingData.RunAsAdmin == '0' ? '关':'开'}}</h5>
      <button class="btn" @click="Setting_Write('RunAsAdmin', '0')">关闭</button>
      <button class="btn" @click="Setting_Write('RunAsAdmin', '1')">开启</button>
      <hr />

      <h5>退出调试模式</h5>
      <button class="btn" @click="Setting_Write('DebugMode', '0')">退出</button>
    </div>
</template>


<script setup>
import { inject } from 'vue'
    const props = defineProps({
        SettingData: Object
    });

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
  function visitDebugAddress() {
    const httpUrl = window.location.origin
    window.location.assign(httpUrl + '/debug')
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

