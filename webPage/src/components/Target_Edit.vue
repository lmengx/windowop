<template>
    <div v-if="target">
        <h2>{{ isEditMode ? '编辑触发词' : '添加触发词' }}</h2>
        <button class="btn" @click="onCancel">取消</button>
        <button class="btn" @click="submitAction = 1">提交</button>
        <h4>触发词</h4>
        <input class="form-control" v-model="target.TargetSign" required placeholder="触发词" />
        <br />
        <h4>忽略词</h4>
        <input class="form-control" v-model="target.PassingSign" required placeholder="忽略词" />
        <br />
        <h4>限定应用程序名</h4>
        <input class="form-control" v-model="target.ApplicationName" required placeholder="应用程序名" />
        <br />
        

        <EditAction ref="editAction"
                    :RawActionCalls="target.Action"
                    :submitAction="submitAction"
                    @SubmitAction="submitAll" />

    </div>
</template>

<script setup>
    import { ref, computed, nextTick } from 'vue';
    import EditAction from './EditAction.vue';

    const props = defineProps({
        target: Object,
    });

    const submitAction = ref(0);
    const emit = defineEmits(['submit', 'cancel']);
    const target = ref({ ...props.target });
    const editActionRef = ref(null); // 用于引用子组件

    function submitAll(ActionCalls) {
        // 清空 Action 数组
        target.value.Action = ActionCalls; // 将 Action 设置为空数组

        console.log(JSON.stringify(target.value, null, 2)); // 检查即将提交的数据的结构
        emit('submit', target.value); // 提交父组件数据
    }



    function onCancel() {
        emit('cancel');
    }

    const isEditMode = computed(() => !!target.value.ID);
</script>
