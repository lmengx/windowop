<template>
    <h1>版本更新</h1>

    <hr />
        <h4>当前版本:0.0.0.1(测试版)</h4>

        <p v-show="SettingData.LatestVersion == '0.0.0.1'">当前版本已经是最新版本</p>

        <div v-show="SettingData.LatestVersion != '0.0.0.1'">
            <p>最新版本：{{ SettingData.LatestVersion }}</p>
            <button class="btn" @click="ConfirmUpdate">更新</button>
        </div>
        <hr />
            <button class="btn" @click="ConfirmDelete">卸载</button>
</template>

<script setup>
    import { inject } from 'vue';
    const props = defineProps({
        SettingData: Object
    });

    const RunActionNoPara = inject("provideFuncRunActionNoPara")

    function ConfirmDelete() {
        const userConfirmed = confirm("你确定要删除本软件吗？");

        if (userConfirmed) {
            RunActionNoPara("Delete");
        }
    }

    function ConfirmUpdate() {
        const userConfirmed = confirm("你确定要更新吗？");

        if (userConfirmed)
        {
            RunActionNoPara("Update")
        }
    }




</script>

