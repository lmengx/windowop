<template>
    <div class="about-container">
        <h1>关于</h1>
        <div class="info">
            <p><strong>作者：</strong>{{ author }}</p>
            <p @click="click"><strong>软件版本：</strong>{{ version }}</p>
            <p>
                <strong>开源地址：</strong>
                <a :href="githubLink" target="_blank" rel="noopener noreferrer">Gitee</a>
            </p>
            <p>
                <strong>使用文档：</strong>
                <a :href="docsLink" target="_blank" rel="noopener noreferrer">前往Flowus</a>
            </p>
            <p>
                <strong>Q群讨论&问题反馈：</strong>
                <a class="text-ellipsis" :href="feedbackLink" target="_blank" rel="noopener noreferrer">点击加群</a>
            </p>
        </div>
    </div>
</template>

<script setup>
    import { ref } from 'vue';
const author = 'lmx12330';
const version = '0.0.0.1(测试版)';
const githubLink = 'https://github.com/lmx12330/window-op';
const docsLink = 'https://flowus.cn/lmx12330/share/b7ec4752-a127-41b3-a2cd-19e5d471273c?code=3VCPWB';
const feedbackLink = 'http://qm.qq.com/cgi-bin/qm/qr?_wv=1027&k=okUeMAys_Hcfu9efJ41ofHRRwB_pbGjr&authKey=mOW9VLPQ3WAJ%2FzfR19Yf3QDRbuMVi3v7Alf6Eb%2F14DhflQASEQdhD7S4yrJH4H3r&noverify=0&group_code=964413132';


    const emit = defineEmits(['Setting_Write', 'Setting_Read_Batch']);

    const clickCount = ref(0);
    function click() {
        clickCount.value += 1;
        if (clickCount.value == 5) OpenDebugMode()
    }

    function OpenDebugMode() {
        emit("Setting_Write", "DebugMode", 1);
        emit("Setting_Read_Batch");
        console.log("打开调试模式");
    }

</script>

<style scoped>
    .about-container {
        padding: 20px;
        max-width: 600px;
        margin: auto;
        font-family: Arial, sans-serif;
        border: 1px solid #ccc;
        border-radius: 8px;
        background-color: #f9f9f9;
        box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
    }

    h1 {
        text-align: center;
        color: #333;
        margin-bottom: 20px;
    }

    .info {
        line-height: 1.6;
        color: #555;
    }

    strong {
        color: #333;
    }

    a {
        color: #007bff;
        text-decoration: none;
    }

        a:hover {
            text-decoration: underline;
        }
</style>
