<template>

    <h1>初始化</h1>



</template>

<script setup>

    import { ref, watch, onMounted } from 'vue';

    const props = defineProps({
        FilePath: String,
        DirContent: Object,

    })

    const emit = defineEmits(['GetPathContent', 'GetDiskContent', 'ReadFile', 'WriteFile', 'RemoveFile', '']);


    watch(
        () => props.receivedEvent,
        (newEvent) => {
            const eventData = newEvent.data; // 获取事件数据
            RecievedMessage(eventData);
        }
    );

    function RecievedMessage(eventData)
    {
        if (eventData.startsWith("ProgramPath:"))
        {
            ProgramPath.value = eventData.slice("ProgramPath:".length);



        }

    }

    onMounted(() => {
        GetProgramPath()
    });
</script>