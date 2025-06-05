<template>
    <div v-auto-animate>
        <h1>控制页面</h1>

        <el-button type="primary" :plain="showPage != 'RunAction'" @click="() => showPage = 'RunAction'">执行命令</el-button>
        <el-button type="primary" :plain="showPage != 'ExpandContent'" @click="() => showPage = 'ExpandContent'">扩展内容</el-button>


        <div v-if=" showPage == 'RunAction'">
            <h2>触发Action</h2>
            <el-button type="success" plain @click="submitAction = 1">提交</el-button>
            <EditAction ref="editAction"
                        :RawActionCalls="[]"
                        :submitAction="submitAction"
                        @SubmitAction="EmitActions" />
        </div>

        <div v-if="showPage == 'ExpandContent'">
            <ExpandContent
                           @SubmitAction="RunActions"
                           />
        </div>

    </div>

</template>

<script setup>
    import { ref } from 'vue';
    import EditAction from './EditAction.vue';
    import ExpandContent from './ExpandContent.vue';

    const showPage = ref('RunAction');
    const RunActions = inject("provideFuncRunActions")


    const submitAction = ref(0)

    function EmitActions(ActionCall)
    {
        RunActions(ActionCall)
        submitAction.value = 0
    }


</script>
