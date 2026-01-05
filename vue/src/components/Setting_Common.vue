<template>
  <h1>常规设置</h1>
  <div>
    <h2>检测间隔(毫秒)</h2>
    <p>建议100--2000之间</p>
    <el-input-number style="width: 240px" v-model="SettingData.SleepTime" :step="100" :min="10" controls-position="right" />
    <el-button type="primary" plain @click="Setting_Write('SleepTime',SettingData.SleepTime)">提交</el-button>
  </div>
  <br />
  <hr />
  <div>
    <el-text size="large">
      开机启动
    </el-text>

    <el-popover placement="right"
                title="注意"
                :width="200"
                trigger="hover"
                content="这个操作需要管理员权限，如果没有，程序会尝试获取">
      <template #reference>
        <el-icon><IEpInfoFilled /></el-icon>
      </template>
    </el-popover>


    <br />
    <el-button type="primary" plain @click="RunActionNoPara('CreateTask_AutoRun')">创建开机启动任务</el-button>
    <el-button type="primary" plain @click="RunActionNoPara('DeleteTask_AutoRun')">移除开机启动任务</el-button>

  </div>
  <br />
  <hr />
  <div>
    <el-text size="large">更改密码</el-text>

  </div>

  <el-button type="primary" plain @click="ChangePwdBox = true">更改密码</el-button>


  <el-dialog v-model="ChangePwdBox"
             title="更改密码"
             width="300"
             :show-close="false"
             center
             >
    <div style=" text-align: center">
      <p>旧密码:</p>
      <el-input style="width: 240px" show-password v-model="oldPassword" />
      <br />
      <p>新密码:</p>
      <el-input style="width: 240px" show-password v-model="newPassword" />
    </div>


    <template #footer>
      <div class="dialog-footer">
        <el-button @click="HidePwdBox">
          取消
        </el-button>
        <el-button type="primary" @click="ChangePwd(oldPassword, newPassword)">
          确认
        </el-button>
      </div>
    </template>

  </el-dialog>





</template>

<script setup>
import { ref, inject, onMounted, onUnmounted} from 'vue';
import { useWebSocketStore } from '@/stores/websocketStore';

  const websocketStore = useWebSocketStore()

onMounted(() => {
  const handler = (data) =>
  {
      if(data.startsWith("PwdChangeSuc:")) HidePwdBox()
  }

  websocketStore.registerMessageHandler(handler)
  const handlerRef = { handler }

  onUnmounted(() => {
    websocketStore.unregisterMessageHandler(handlerRef.handler)
  })
})




    const ChangePwdBox = ref(false)

    function HidePwdBox()
    {
        ChangePwdBox.value = false;
       oldPassword.value = '';
       newPassword.value = '';
    }

    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);

    const oldPassword = ref('');
    const newPassword = ref('');

    const props = defineProps({
        SettingData: Object
    });

    const RunActionNoPara = inject("provideFuncRunActionNoPara")
  const ChangePwd = websocketStore.ChangePwd


    function Setting_ReadAll() {
        emit("Setting_Read_Batch");
    }

    function Setting_Write(item, value) {
        emit("Setting_Write", item, value);
        Setting_ReadAll();
    }

</script>

