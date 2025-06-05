<template>
  <div class="fullscreen centerbox" v-loading="!Disconnected">
    <div v-show="Disconnected" style=" text-align: center">
      <el-text>无法连接，请检查网络后刷新页面。</el-text>
      <br />
      <el-button @click="retryConnection">重试连接</el-button>
    </div>
  </div>
</template>

<script setup>

    const props = defineProps({
        Disconnected: Boolean,
    });

    function retryConnection() {
        location.reload(); // 重新加载当前页面
    }
</script>
