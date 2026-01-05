<template>

  <el-container class="common-layout fullscreen">

    <el-menu default-active="2"
             class="el-menu"
             :collapse="isCollapse"
              >

      <el-menu-item index="1" @click="ChangeCollapseMode">
        <el-icon><IEpSwitch /></el-icon>
      </el-menu-item>

      <el-menu-item index="2" @click="RouterPush('/dashboard')">
        <el-icon><IEpCollection /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">首页</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="3" @click="RouterPush('/dashboard/remotedesk')">
        <el-icon><IEpConnection /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">远程桌面</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="4" @click="RouterPush('/dashboard/scriptmanager/targetlist')">
        <el-icon><IEpSetUp /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">脚本</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="5" @click="RouterPush('/dashboard/filemanager')">
        <el-icon><IEpFolderOpened /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">文件管理</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="6" @click="RouterPush('/dashboard/setting')">
        <el-icon><IEpSetting /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">设置</el-text>
          </el-button>
        </template>
      </el-menu-item>

      <el-menu-item index="7" @click="LogoutDialogVisible = true">
        <el-icon><IEpLock /></el-icon>
        <template #title>
          <el-button type="" plain text>
            <el-text class="mx-1" size="large">断开连接</el-text>
          </el-button>
        </template>
      </el-menu-item>
    </el-menu>



    <el-main v-auto-animate>


      <router-view />


      <Setting v-if="false" />
    </el-main>



  <el-dialog
    v-model="LogoutDialogVisible"
    title="断开连接"
    width="500"
    align-center
  >

        <span>确定要断开连接吗？</span>
        <br />
        <div class="right-aligned" v-show="isSavePwd">
          <input type="checkbox" v-model="clearPwd" />
        <span>清除密码</span>
        </div>

    <template #footer>
      <div class="dialog-footer">
        <el-button @click="LogoutDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="Logout">
          确认
        </el-button>
      </div>
    </template>
  </el-dialog>



  </el-container>

</template>

<script setup>

  import { ref, provide, watch, computed } from 'vue';
  import { useRouter } from 'vue-router'
  import { useDataStore } from '../stores/dataStore.js'
  import { useWebSocketStore } from '@/stores/websocketStore.js';

  import { useWindowWidth } from '@/composables/useWindowWidth'

const router = useRouter()

function RouterPush(target)
{
  router.push(target)
}


  const { windowWidth } = useWindowWidth()

  const collapseMode = ref(null)

  const isCollapse = ref(false)

  const LogoutDialogVisible = ref(false)

  const isSavePwd = computed(() => {
        const targetName = websocketStore.Target.name;

  const target = DataStore.ConnectTargets.find(t => t.name === targetName)
  if (!target) return false
  if(target.HashedPwd != '' && target.HmacKey != '') return true
    return false
});

watch(windowWidth, (newVal) => {
  watchCollapse(newVal)
})


  function watchCollapse(newVal)
  {
        if(collapseMode.value == null)
    {
      if(newVal >= 960) isCollapse.value = false
      else isCollapse.value = true;

    }
    else isCollapse.value = collapseMode.value
  }

  function ChangeCollapseMode()
  {
    if(collapseMode.value == null)
      if(windowWidth >= 960) collapseMode.value = false;
      else collapseMode.value = true;
    else collapseMode.value = !collapseMode.value
    watchCollapse(windowWidth)
  }

  const DataStore = useDataStore()
  const websocketStore = useWebSocketStore()

    const SendMsg = websocketStore.sendMessage
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

    const clearPwd = ref(false)

  function Logout()
  {
    const clearName = DataStore.ConnectTargets.name

    if(clearPwd.value) DataStore.SavePwd(clearName, '','')
    websocketStore.close()
    router.push("/connect")


  }


</script>

<style scoped>
  .el-menu:not(.el-menu--collapse) {
    width: 200px;
    height: 100%;
  }
.right-aligned {
  margin-left: auto;
}
</style>
