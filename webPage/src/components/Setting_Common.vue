<template>
    <h1>常规设置</h1>
    <div>
        <h2>检测间隔(毫秒)</h2>
        <p>建议100--2000之间</p>
        <input class="form-control" v-model="SettingData.SleepTime" placeholder="输入间隔"/>
        <button class="btn" @click="Setting_Write('SleepTime',SleepTime)">提交</button>
    </div>

    <div>
        <h2>开机启动</h2>
        <button class="btn" @click="RunActionNoPara('CreateTask_AutoRun')">创建开机启动任务</button>
        <button class="btn" @click="RunActionNoPara('DeleteTask_AutoRun')">移除开机启动任务</button>

    </div>

    <div>
        <h2>更改密码</h2>
        <p>旧密码:</p>
        <input class="form-control" type="password" v-model="oldPassword" />
        <p>新密码:</p>
        <input class="form-control" type="password" v-model="newPassword" />
        <br />
        <button class="btn" @click="changePassword">提交</button>
    </div>


</template>

<script setup>
    import { ref, inject} from 'vue';

    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);

    const oldPassword = ref('');
    const newPassword = ref('');

    const props = defineProps({
        SettingData: Object
    });

    const RunActionNoPara = inject("provideFuncRunActionNoPara")
  const SendMsg = inject("provideFuncSendWSMsg")
  const ChangePwd = inject("provideFuncChangePwd")
  

    function Setting_ReadAll() {
        emit("Setting_Read_Batch");
    }

    function Setting_Write(item, value) {
        emit("Setting_Write", item, value);
        Setting_ReadAll();
    }


    function changePassword()
    {
      ChangePwd(oldPassword.value, newPassword.value)
            oldPassword.value = '';
            newPassword.value = '';

    }


</script>

