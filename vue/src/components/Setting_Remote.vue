<template>


  <div v-auto-animate>

    <br /><hr />

    <h1>局域网服务</h1>

    <el-popover placement="right"
                title="注意"
                :width="200"
                trigger="hover"
                content="这个操作需要管理员权限">
      <template #reference>
        <el-text>
          局域网服务<el-icon><IEpInfoFilled /></el-icon>
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
    <el-button type="primary" plain @click="UAC('0')">尝试关闭UAC</el-button>
    <el-button type="primary" plain @click="UAC('1')">恢复UAC</el-button>
    <br /><br />


    <hr />
    <h1>公网服务</h1>

    <el-text>
      内网穿透
    </el-text>

    <el-switch v-model="Frp_Enable"
               :before-change="() =>{return true}"
               @click="Setting_Write('Frp_Enable', SettingData.Frp_Enable ? 0:1)" />

    <h5>Frpc启动参数</h5>
    <el-input style="width: 240px" v-model="SettingData.Frp_parameters" placeholder="-f xxxxxxxxxx:12345678" />
    <el-button type="primary" plain @click="Setting_Write('Frp_parameters', SettingData.Frp_parameters)">确认</el-button>
    <br /><br />

    <strong>内置Frp使用方法：</strong>

    <a :href="helpLink" target="_blank" rel="noopener noreferrer">查看文档</a>


    <hr />


  </div>
</template>


<script setup>
import { inject } from 'vue'
    const props = defineProps({
        SettingData: Object
    });

  const ServeAllPrefix = ref(false)
  const RunAsAdmin = ref(false)

  const Frp_Enable = ref(false)

  const helpLink = 'https://flowus.cn/lmx12330/2f324cd1-9437-4a44-b757-89a124751b88';



  onMounted(() => {
    SetRef(props.SettingData)
  });

  watch(props.SettingData, (newValue) => {
    SetRef(newValue)
  })

  function SetRef(newValue) {
    ServeAllPrefix.value = newValue.ServeAllPrefix == 1
    RunAsAdmin.value = newValue.RunAsAdmin == 1
    Frp_Enable.value = newValue.Frp_Enable == 1

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

