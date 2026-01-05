
<template>
  <div class="fullscreen">
    <el-container>

      <el-header v-if="route.path == '/' || route.path == '/connect'">
        <el-affix :offset="0.00001">
          <AppHeader />
        </el-affix>
      </el-header>

    <el-main>
      <router-view v-slot="{ Component, route }">
            <transition
              name="page"
              mode="out-in"
              appear
            >
          <component
            :is="Component"
            :key="route.fullPath"
          />
        </transition>
      </router-view>
    </el-main>

    <el-footer class="footer" v-if="route.path == '/'">
      <hr />
      <span>Released under the <a href="https://opensource.org/licenses/MIT" target="_blank" rel="noopener noreferrer">MIT License</a>.</span>
  <span>Powered by <a href="https://cn.vuejs.org/" target="_blank" rel="noopener noreferrer">Vue3</a></span>
    </el-footer>

    </el-container>
  </div>

</template>


<script setup>
  import { ref, onMounted, onUnmounted, watch } from 'vue';
  import { useRoute } from 'vue-router'
  import { useRouter } from 'vue-router'


  import { useDataStore } from './stores/dataStore.js'
  import { useWebSocketStore } from '@/stores/websocketStore'

  import AppHeader from './components/Header.vue'

const route = useRoute()
const router = useRouter()


const websocketStore = useWebSocketStore()
const DataStore = useDataStore()

watch(
  () => route.path,
  (newPath) => {
    if (newPath === '/connect') return    // 跳过 connect 页面本身（避免无限跳转）
    if (newPath !== '/' && !websocketStore.Verified) {    // 如果未验证，且不在 / 或 /connect，则跳转
      ElNotification({
        title: "请重新连接",
        type: "info",
      })
      router.push('/connect')
    }
  },
  { immediate: true } // 立即检查当前路由
)



onMounted(() => {
  const handler = (data) =>
  {
      RecievedMsg(data);
  }  // 注册特定类型消息的处理

  websocketStore.registerMessageHandler(handler) // 将处理器注册到WebSocket Store
  const handlerRef = { handler }  // 保存处理器引用，以便卸载时注销

  onUnmounted(() => {
    websocketStore.unregisterMessageHandler(handlerRef.handler)  // 组件卸载时注销处理器
  })
})

onUnmounted(() => {
  websocketStore.close()
})

    function RecievedMsg(eventJson)
    {
      if (eventJson == null) return;

        else if (eventJson.Operation == "Notification") {
          ElNotification({
            title: eventJson.title,
            message: eventJson.message,
            type: eventJson.type,
          })
        }

        else if (eventJson.Operation == "Download") {
          const ws1 = new DownloadWS('');//判断类型，文件夹则为zip

        }

        else if (eventJson.Operation == "Error") {
          console.error(eventJson.message);
        }

    }


</script>

<style>
/* 页面切换动画：淡入 + 微微上移 */
.page-enter-active {
  transition: all 0.35s ease;
}
.page-leave-active {
  transition: all 0.25s ease;
}
.page-enter-from {
  opacity: 0;
  transform: translateY(10px);
}
.page-leave-to {
  opacity: 0;
  transform: translateY(-10px);
}
.footer{
  display: flex;
  justify-content: center;
  align-items: center;
  gap: 16px;
}
</style>
