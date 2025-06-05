<template>

  <el-container class="common-layout fullscreen">

    <el-menu default-active="2"
             class="el-menu"
             :collapse="isCollapse"
             @open=""
             @close="">

      <el-menu-item index="1" @click="() => isCollapse = !isCollapse">
        <el-icon><IEpSwitch /></el-icon>
      </el-menu-item>

      <el-menu-item index="2" @click="() => showPage = 'TargetList'">
        <el-icon><IEpCollection /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">检测规则</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="3" @click="() => showPage = 'RemoteDesk'">
        <el-icon><IEpConnection /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">远程桌面</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="4" @click="() => showPage = 'Pannel'">
        <el-icon><IEpSetUp /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">控制面板</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="5" @click="() => showPage = 'FileManager'">
        <el-icon><IEpFolderOpened /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">文件管理</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="6" @click="() => showPage = 'Setting'">
        <el-icon><IEpSetting /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">设置</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="7" v-show="PwdStore.HashedPwd != ''" @click="Logout">
        <el-icon><IEpLock /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">锁定</el-text>
          </el-button>
        </template>
      </el-menu-item>
    </el-menu>

    

    <el-main v-auto-animate>
      
      <RemoteDesk v-if="showPage == 'RemoteDesk'" />

      <TargetList v-if="showPage == 'TargetList'" />

      <Pannel v-if="showPage == 'Pannel'" />

      <FileManager_core mode="Operation" v-if="showPage == 'FileManager'" />

      <Setting v-if="showPage == 'Setting'" />
    </el-main>
  </el-container>


</template>

<script setup>

  import { ref, provide, inject } from 'vue';
  import { usePwdStore } from '../stores/Pwd.js'

  import RemoteDesk from './RemoteDesk.vue';
  import TargetList from './Target_List.vue';
    import Pannel from './Pannel.vue';
    import Setting from './Setting.vue';
    import FileManager_core from './FileManager_core.vue';

  const showPage = ref("TargetList");
  const isCollapse = ref(false)

  onMounted(() => {
    if (window.innerWidth < 500) isCollapse.value = true
  });

  const PwdStore = usePwdStore()

    const SendMsg = inject("provideFuncSendWSMsg")
    provide("provideFuncRunActions",  RunActions)
    provide("provideFuncRunActionNoPara", RunActionNoPara)

    function RunActions(ActionCalls) {
      const reqObj = {
        Operation: "RunAction",
        Action: ActionCalls
      };
      const reqJson = JSON.stringify(reqObj);
      SendMsg(reqJson)
    }
    function RunActionNoPara(ActionName)
    {
      const reqObj = [{
        ActionName: ActionName,
        Paras: []
      }];
      const ActionCalls = JSON.stringify(reqObj);

        RunActions(ActionCalls)
    }

    

  function Logout() {

    ElMessageBox.confirm(
    '要退出登录吗?',
    '确认',
    {
      confirmButtonText: '确认',
      cancelButtonText: '取消',
      type: 'warning',
    }
  )
    .then(() => {

    PwdStore.HashedPwd = ""
    PwdStore.HmacKey = ""
    location.reload();

    })
    .catch(() => {

    })
  }


</script>

<style scoped>
  .el-menu:not(.el-menu--collapse) {
    width: 200px;
    height: 100%;
  }
</style>
