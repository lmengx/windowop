<template>
    <div class="loading-overlay" v-if="!Disconnected">
        <p>正在加载，请稍候...</p>
        <div class="loader"></div> <!-- 加载动画 -->
    </div>

    <div v-else>
        <p>无法连接，请检查网络后刷新页面。</p>
        <button @click="retryConnection">重试连接</button>
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

<style scoped>
    .loading-overlay {
        position: fixed; /* 固定定位 */
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(255, 255, 255, 0.8); /* 半透明背景 */
        display: flex;
        flex-direction: column; /* 竖向排列内容 */
        justify-content: center; /* 垂直居中 */
        align-items: center; /* 水平居中 */
        z-index: 1000; /* 确保覆盖在其他元素上 */
    }

    .loader {
        border: 8px solid #f3f3f3; /* Light grey */
        border-top: 8px solid #3498db; /* Blue */
        border-radius: 50%;
        width: 50px; /* 动画大小 */
        height: 50px;
        animation: spin 1s linear infinite; /* 动画效果 */
    }

    @keyframes spin {
        0% {
            transform: rotate(0deg);
        }

        100% {
            transform: rotate(360deg);
        }
    }
</style>