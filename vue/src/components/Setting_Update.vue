<template>
  <h1>版本更新</h1>

  <hr />
  <h4>当前版本:0.0.0.1(测试版)</h4>
  <p v-show="SettingData.LatestVersion != version">最新版本：{{ SettingData.LatestVersion }}</p>
  <p v-show="SettingData.LatestVersion == version">当前版本已经是最新版本</p>


  <el-button type="warning" plain @click="ConfirmUpdate">{{ SettingData.LatestVersion == version ? '重装' : '更新' }}</el-button>
  <el-button type="danger" plain @click="ConfirmDelete">卸载</el-button>

  <el-dialog v-model="DeleteDialogVisible" title="确定要卸载本软件吗" width="500" center>
    <template #footer>
      <div class="dialog-footer">
        <el-button @click="DeleteDialogVisible = false">取消</el-button>
        <el-button type="danger" @click="RunActionNoPara('Delete')">
          确定
        </el-button>
      </div>
    </template>
  </el-dialog>

</template>

<script setup>
  import { inject } from 'vue';

    const version = ref("0.0.0.1")

    const props = defineProps({
        SettingData: Object
    });

    const RunActionNoPara = inject("provideFuncRunActionNoPara")

  const DeleteDialogVisible = ref(false)

  function ConfirmUpdate() {
    const msgWord = (props.SettingData.LatestVersion == version.value) ? '重装' : '更新'
          ElMessageBox.confirm(
    `要${msgWord}吗?`,
    msgWord,
    {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'warning',
    }
  )
    .then(() => {

    RunActionNoPara('Update')
      ElNotification({
        title: "已发起" + msgWord,
        type: "success",
      })
    })
    .catch(() => {
      ElNotification({
        title: "已取消" + msgWord,
        type: "error",
      })
    })
    }


    function ConfirmDelete(){
    ElMessageBox.confirm(
    '要删除本软件吗?',
    '卸载',
    {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'error',
    }
  )
    .then(() => {

    RunActionNoPara('Delete')
      ElNotification({
        title: "软件正在卸载",
        type: "success",
      })
    })
    .catch(() => {
      ElNotification({
        title: "已取消卸载",
        type: "error",
      })
    })
    }




</script>

