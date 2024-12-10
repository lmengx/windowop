<template>
    <div class="alert-container">
        <transition-group name="fade" tag="div">
            <div v-for="(item, index) in modalData" :key="index" class="alert">
                <p>{{ item }}</p>
            </div>
        </transition-group>
    </div>
</template>

<script>
    import { ref } from 'vue';

    export default {
        setup() {
            const modalData = ref([]); // 存储当前显示的消息
            const queue = ref([]); // 队列

            // 添加新消息到队列
            const addToQueue = (newData) => {
                queue.value.push(newData);
                showNext(); // 如果没有正在显示的消息，尝试显示下一个消息
            };

            // 显示下一个消息
            const showNext = () => {
                if ( queue.value.length === 0) {
                    return; // 如果没有新消息，则直接返回
                }

                const nextMessage = queue.value.shift(); // 从队列中取出第一个消息
                modalData.value.push(nextMessage); // 将新消息加入显示

                // 在3秒后移除当前消息
                setTimeout(() => {
                    modalData.value.shift(); // 移除第一个消息
                    showNext(); // 如果还有排队的消息，显示下一个
                }, 3000);
            };

            return {
                modalData,
                addToQueue,
            };
        },
    };
</script>

<style scoped>
    .alert-container {
        position: fixed;
        bottom: 20px; /* 固定在页面底部 */
        left: 20px; /* 距离左边一定距离 */
        z-index: 1000; /* 确保在最上面 */
    }

    .alert {
        background: white;
        padding: 15px; /* 提示框内边距 */
        margin-top: 10px; /* 消息之间的间隔 */
        border-radius: 5px; /* 圆角 */
        box-shadow: 0 0 10px rgba(0, 0, 0, 0.2); /* 阴影效果 */
        transition: opacity 0.3s, transform 0.3s; /* 动画效果 */
    }

    .fade-enter-active, .fade-leave-active {
        transition: opacity 0.5s; /* 过渡效果 */
    }

    .fade-enter, .fade-leave-to {
        opacity: 0; /* 进入/离开时透明度 */
    }
</style>