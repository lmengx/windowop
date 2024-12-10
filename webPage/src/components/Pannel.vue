<template>
    <div>
        <h1>控制页面</h1>

        <button class="btn" @click="() => showPage = 'RunAction'">执行命令</button>
        <button class="btn" @click="() => showPage = 'ExpandContent'">扩展内容</button>


        <div v-if=" showPage == 'RunAction'">
            <h2>触发Action</h2>
            <button class="btn"  @click="submitAction = 1">提交</button>
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
