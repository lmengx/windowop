<template>


  <div v-if="editID === 0" v-auto-animate>
    <h2>检测规则</h2>
    <el-button type="primary" plain @click="queryTargetList(currentPage)">刷新</el-button>
    <el-button type="primary" plain @click="startCreating">添加规则</el-button>

    <br /><br />

    <ul v-auto-animate>
      <li v-for="target in paginatedTargets" :key="target.ID">
        <div>
          <p>触发词： {{ target.TargetSign }} </p>
          <p>忽略词： {{ target.PassingSign }} </p>
          <p>限定应用程序名： {{ target.ApplicationName }}</p>
          <el-button color="#626aef" plain circle @click="startEdit(target)"><el-icon><IEpEdit /></el-icon></el-button>
          <el-button :type="target.Enable == '1' ? 'success' : 'warning'"
                     plain
                     circle
                     @click="toggleEnable(target, target.Enable == 0 ? 1 : 0)">
            <el-icon v-auto-animate>
              <IEpCloseBold v-if="target.Enable == 0" />
              <IEpSelect v-else />
            </el-icon>
          </el-button>

          <el-popconfirm title="删除规则"
                         @confirm="deleteTarget(target.ID)">
            <template #reference>
              <el-button type="danger" plain circle> <el-icon><IEpDelete /></el-icon> </el-button>
            </template>
          </el-popconfirm>

        </div>
      </li>
    </ul>

      <el-pagination background layout="prev, pager, next" :hide-on-single-page="true" v-model:current-page="currentPage" :page-count="totalPages" />
    


    <el-empty v-show="targetList.length == 0" description="还没有创建规则" />

  </div>


        <div v-if="editID === -1">
            <EditTarget :target="newTarget"
                        @submit="addTarget"
                        @cancel="cancelEdit" />
        </div>

        <div v-if="editID > 0">
            <EditTarget :target="currentTarget"
                        @submit="updateTarget"
                        @cancel="cancelEdit" />
        </div>



</template>

<script setup>
    import { ref, onMounted, watch, computed, inject } from 'vue';
    import EditTarget from './Target_Edit.vue';


    const receivedEvent = inject("provideReceivedMsg")
    const SendMsg = inject("provideFuncSendWSMsg")

    const newTarget = ref({ TargetSign: '', PassingSign: '', ApplicationName: '', Action: ''});
    const currentTarget = ref({ TargetSign: '', PassingSign: '', ApplicationName: '', Action: '' });
    const targetList = ref([]);
    const editID = ref(0); // -1: 新建, 0: 不编辑, >0: 编辑的目标ID

    const currentPage = ref(1);
    const itemsPerPage = 5;

    onMounted(() => {
      queryTargetList(currentPage.value); // 初始化时加载当前页数据
    });

    watch(receivedEvent, (newValue) => {
                receivedMessage(newValue.text)
            })

    function receivedMessage(eventData) {
        if (eventData.startsWith("TargetList:")) {
            const jsonString = eventData.slice("TargetList:".length);
            // 直接解析并赋值
            targetList.value = JSON.parse(jsonString);

            // 更新页面，确保当前页不会超过总页数
            if (currentPage.value > totalPages.value && totalPages.value > 0) {
                currentPage.value = totalPages.value; // 如果当前页数超出最大页数，自动设置为最大页数
            }
        }
    }

    function ReadToMemory(item) {
        const reqJson = `{"Operation": "ReadToMemory",
                            "Item": "${item}"
                        }`;
       SendMsg(reqJson)
    }

  function queryTargetList(page = 1) {
        const reqJson = `{"Operation": "QuiryTargetList"}`;
        SendMsg(reqJson)

        // 初始化页面时设置默认页
        currentPage.value = page;
    }

    function startCreating() {
        editID.value = -1; // 设置为新建状态
        newTarget.value = { TargetSign: '', PassingSign: '', Action: '', ApplicationName: '' }; // 为新建清空
    }

    function startEdit(target) {
        editID.value = target.ID;
        currentTarget.value = { ...target };
    }

    function addTarget(target) {
        const ActionStr = JSON.stringify(target.Action, null, 2);
        const reqJson = `{
            "Operation": "AddTarget",
            "TargetSign": "${target.TargetSign}",
            "PassingSign": "${target.PassingSign}",
            "ApplicationName": "${target.ApplicationName}",
            "Action": ${ActionStr}
        }`;
        SendMsg(reqJson)


        // 确保在目标添加后，如果当前页面已满，则跳转到下一页
        const nextPage = Math.ceil((targetList.value.length + 1) / itemsPerPage);
        queryTargetList(targetList.value.length % itemsPerPage === 0 ? nextPage : currentPage.value);
        cancelEdit(); // 取消编辑
        ReadToMemory("TargetList");
    }

    function updateTarget(target) {
        const ActionStr = JSON.stringify(target.Action, null, 2);
        const reqJson = `{
            "Operation": "UpdateTarget",
            "Id": "${target.ID}",
            "TargetSign": "${target.TargetSign}",
            "PassingSign": "${target.PassingSign}",
            "ApplicationName": "${target.ApplicationName}",
            "Action": ${ActionStr}
        }`;
        SendMsg(reqJson)

        queryTargetList(currentPage.value); // 保持在当前页
        editID.value = 0; // 结束编辑
        ReadToMemory("TargetList");
    }

    function toggleEnable(target, enable) { // 修改为 toggleEnable 函数
        const reqJson = `{"Operation": "EnableTarget", "Id": "${target.ID}", "Enable": "${enable}"}`;
        SendMsg(reqJson)

        queryTargetList(currentPage.value); // 刷新列表保持原页
        ReadToMemory("TargetList");

    }

    function cancelEdit() {
        editID.value = 0; // 返回到列表状态
        currentTarget.value = { TargetSign: '', PassingSign: '', Action: '' };
        newTarget.value = { TargetSign: '', PassingSign: '', Action: '' };
    }

    function deleteTarget(id) {
        const reqJson = `{"Operation": "DelTarget", "Id": "${id}"}`;
        SendMsg(reqJson)


        // 删除后查询目标列表并调整页数
        queryTargetList(currentPage.value); // 刷新列表保持原页
        if (targetList.value.length % itemsPerPage === 0 && currentPage.value > totalPages.value) {
            currentPage.value = totalPages.value; // 如果删除后当前页超过最大页数，调整为最大页数
        }
        ReadToMemory("TargetList");

    }

    const paginatedTargets = computed(() => {
        const start = (currentPage.value - 1) * itemsPerPage;
        const end = start + itemsPerPage;
        return targetList.value.slice(start, end);
    });

    const totalPages = computed(() => {
        return Math.ceil(targetList.value.length / itemsPerPage);
    });

    function nextPage() {
        if (currentPage.value < totalPages.value) {
            currentPage.value++;
        }
    }

    function prevPage() {
        if (currentPage.value > 1) {
            currentPage.value--;
        }
    }
</script>

<style scoped>
    .pagination {
        margin-top: 10px;
    }
</style>
