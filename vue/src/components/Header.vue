<template>
  <el-header class="app-header">
    <!-- 左侧：Logo + 应用名 -->
    <div class="header-left">

      <RouterLink class="app-title" to="/">

        windowOP
      </RouterLink>
    </div>



    <!-- 右侧：操作按钮 -->
    <div class="header-right">


      <el-button
        text
        @click="openGitHub"
        title="View on GitHub"
      >
        <img viewBox="0 0 24 24" width="18" height="18" fill="currentColor" style="margin : 6px" :src="githubSVG">
        <span>GitHub</span>
      </el-button>
    </div>
  </el-header>
</template>

<script setup>
import { ref } from 'vue'
import githubSVG from '@/assets/github.svg'




// 右侧操作

const openGitHub = () => {
  window.open('https://github.com/lmx12330/window-op/', '_blank')
}
</script>

<style scoped>
.app-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 24px;
  height: 60px;
  background-color: #ffffff;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.1);
  border-bottom: 1px solid var(--el-border-color-light);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.app-icon {
  color: #409eff;
}

.app-title {
  font-size: 18px;
  font-weight: 600;
  color: #333;
}


.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.header-right .el-button {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 14px;
  color: #666;
  padding: 6px 10px;
}

.header-right .el-button:hover {
  color: #409eff;
  background: transparent;
}
</style>
