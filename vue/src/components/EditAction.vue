<template>
  <div v-auto-animate>

    <el-tabs>

        <el-tab-pane v-for="ActionList in ActionLists" :label="ActionList.ListName" :name="ActionList.ListName">

          <el-popconfirm v-for="ActionProj in ActionList.Actions"
                         title="添加Action"
                         @confirm="addAction(ActionProj)">
            <template #reference>
              <el-button> {{ ActionProj.Profile }}</el-button>
            </template>
          </el-popconfirm>

        </el-tab-pane>
    </el-tabs>

    <br /><hr />

    <div>
      <h4>已添加的操作:</h4>
      <ul v-auto-animate>
        <li v-for="(ActionCall, index) in ActionCalls" :key="ActionCall.id">

          <p>{{ getActionProfile(ActionCall.ActionName) }}</p>
          <div v-for="(Para, paraIndex) in ActionCall.Paras" :key="paraIndex">
            <label>{{ getParameterProfile(ActionCall.ActionName, Para.ParaName) }}:</label>
            <br />
            <el-input v-model="Para.value"
                      style="width: 240px"
                      :rows="2"
                      type="textarea"
                      :placeholder="Para.ParaName" />
          </div>
          <el-button type="primary" @click="moveAction(index, -1)" :disabled="index === 0">上移</el-button>
          <el-button type="primary" @click="moveAction(index, 1)" :disabled="index === ActionCalls.length - 1">下移</el-button>
          <el-button type="danger" @click="removeAction(ActionCall.id)">删除</el-button>
        </li>
      </ul>

    <el-empty v-show="ActionCalls.length == 0" description="还没有添加操作" />
    </div>

  </div>
</template>

<script setup>
    import { ref, watch, computed } from 'vue';

    const props = defineProps({
        submitAction: {
            type: Number,
            required: true
        },
        RawActionCalls: {
            required: true
        }
    });

    
  const emit = defineEmits(['SubmitAction']);


  const ActionPreview = ref(null);

  //常规、高级

  const ActionLists = ref([
    {
      ListName: "常规",
      Actions: [
        {
          ActionName: "ctrl_w",
          Profile: "按下Ctrl+w（关闭前台页面）",
          Paras: []
        },
        {
          ActionName: "alt_f4",
          Profile: "按下alt_f4（关闭前台窗口）",
          Paras: []
        },
        {
          ActionName: "OpenWebPage",
          Profile: "打开网页",
          Paras: [{ ParaName: "url", Profile: "目标链接" }]
        },
        {
          ActionName: "msgbox",
          Profile: "弹出文本框",
          Paras: [
            { ParaName: "text", Profile: "显示内容" },
            { ParaName: "title", Profile: "标题" }
          ]
        },
        {
          ActionName: "shutdown",
          Profile: "关机",
          Paras: [{ ParaName: "time", Profile: "延迟时间（毫秒）" }]
        },
        {
          ActionName: "reboot",
          Profile: "重启",
          Paras: [{ ParaName: "time", Profile: "延迟时间（毫秒）" }]
        },
        {
          ActionName: "sleep",
          Profile: "睡眠",
          Paras: [{ ParaName: "time", Profile: "延迟时间（毫秒）" }]
        },
        {
          ActionName: "downloadAndExecute",
          Profile: "下载文件并打开",
          Paras: [
            { ParaName: "url", Profile: "目标URL" },
            { ParaName: "fileType", Profile: "文件拓展名" }
          ]
        },
        {
          ActionName: "wait",
          Profile: "等待",
          Paras: [
            { ParaName: "time", Profile: "等待时间（毫秒）" },
          ]
        },
      ]
    },
    {
      ListName: "高级",
      Actions: [
        {
          ActionName: "cmd",
          Profile: "执行cmd命令",
          Paras: [
            { ParaName: "command", Profile: "执行内容" }
          ]
        },
        {
          ActionName: "RunBatchAsAdmin",
          Profile: "执行cmd命令--管理员身份",
          Paras: [
            { ParaName: "command", Profile: "执行内容" }
          ]
        },
        {
          ActionName: "ExecuteVbs",
          Profile: "运行vbs脚本",
          Paras: [
            { ParaName: "text", Profile: "执行内容" }
          ]
        },
        {
          ActionName: "RunPs",
          Profile: "执行power shell命令",
          Paras: [
            { ParaName: "psContent", Profile: "执行内容" }
          ]
        },
      ]
    },
    {
      ListName: "windowOP",
      Actions: [
        {
          ActionName: "OpenManagePage",
          Profile: "在主机上打开管理页面",
          Paras: []
        },
        {
          ActionName: "OpenManagePage_LANAddress",
          Profile: "在主机上打开管理页面--局域网地址",
          Paras: []
        }
      ]
    }
  ])



    const RawJson = computed(() => {
        try {
            const parsedData = JSON.parse(props.RawActionCalls);
            return Array.isArray(parsedData) ? parsedData : [];
        } catch (e) {
            return [];
        }
    });

    // 初始化 ID 为 1（自增的起始值）
    const currentId = ref(1);

    // 初始化 ActionCalls，并为 RawActionCalls 中的每个操作生成唯一 ID
    const ActionCalls = ref(RawJson.value.map(action => ({
        id: currentId.value++, // 给每个已有操作生成 ID
        ActionName: action.ActionName,
        Paras: action.Paras || []
    })));

    // 添加 Action 调用
    function addAction(action) {
        const actionData = {
            id: currentId.value++, // 生成一个新的 ID 并自增
            ActionName: action.ActionName,
            Paras: action.Paras.map(param => ({
                ParaName: param.ParaName,
                value: ''
            }))
        };

        ActionCalls.value.push(actionData); // 存储到 ActionCalls 中
    }

    // 删除 Action 调用
    function removeAction(id) {
        const index = ActionCalls.value.findIndex(action => action.id === id);
        if (index !== -1) {
            ActionCalls.value.splice(index, 1); // 从 ActionCalls 中移除指定操作
        }
    }

    // 移动 Action 调用
    function moveAction(index, direction) {
        if (direction === -1 && index > 0) { // 向上移动
            const temp = ActionCalls.value[index - 1];
            ActionCalls.value[index - 1] = ActionCalls.value[index];
            ActionCalls.value[index] = temp;
        } else if (direction === 1 && index < ActionCalls.value.length - 1) { // 向下移动
            const temp = ActionCalls.value[index + 1];
            ActionCalls.value[index + 1] = ActionCalls.value[index];
            ActionCalls.value[index] = temp;
        }
    }

  function getActionProfile(actionName) {
    for (const list of ActionLists.value) {
      const action = list.Actions.find(item => item.ActionName === actionName);
      if (action) {
        return action.Profile;
      }
    }
    return '';
  }

  // 通过 ActionName 和 ParaName 获取对应的参数 Profile
  function getParameterProfile(actionName, paraName) {
    for (const list of ActionLists.value) {
      const action = list.Actions.find(item => item.ActionName === actionName);
      if (action) {
        const parameter = action.Paras.find(para => para.ParaName === paraName);
        return parameter ? parameter.Profile : '';
      }
    }
    return '';
  }

    // 监听 props.submit 的变化
    watch(() => props.submitAction, (newValue) => {
        if (newValue === 1) {
            // 提交数据时去掉 id 属性
            const ActionSubmit = ActionCalls.value.map(({ id, ...rest }) => rest); // 去掉 id
            emit('SubmitAction', ActionSubmit); // 发送 ActionSubmit 数据
        }
    });
</script>
