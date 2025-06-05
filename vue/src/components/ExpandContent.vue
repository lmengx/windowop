<template>
    <div>
        <h3>拓展内容</h3>

        <ul>
            <li v-for="(SonList, index) in MainListJson" :key="index">
                <el-button type="primary" v-if="SonList.Name && SonList.Url" @click="ShowSonList(SonList)">
                    {{ SonList.Name }}
                </el-button>
            </li>
        </ul>

      <br />

        <div v-show="ShowSonListJson.length > 0">
          <el-text size="large">{{ ShowSonListName }}</el-text>
          <br />

          <el-popconfirm v-for="(childItem, index) in ShowSonListJson" :key="index""
                         title="确定执行吗?"
                         @confirm="RunActions(childItem.Action)">
            <template #reference>
              <el-button type="primary" plain>
                {{ childItem.Name }}
              </el-button>
            </template>
          </el-popconfirm>

        </div>
    </div>
</template>

<script setup>
    import { ref, onMounted, watch, inject } from 'vue';

  const SendMsg = inject("provideFuncSendWSMsg")
  const RunActions = inject("provideFuncRunActions")
  const receivedEvent = inject("provideReceivedMsg")

  watch(receivedEvent, (newValue) => {
    receivedMessage(newValue.text)
  })

    const MainListUrl = 'https://gitee.com/lmx12330/window-op/raw/master/resource/expand/ExpandList.json';
    const MainListJson = ref([]);  // 主列表数据

    const ShowSonListName = ref('');
    const ShowSonListUrl = ref('');
    const ShowSonListJson = ref([]);  // 子列表数据

    // 从指定 URL 获取文本
  function GetTextFromUrl(url) {
    const reqJson = JSON.stringify({
      Operation: "GetTextFronUrl",
      Url: url
    });
    SendMsg(reqJson)
  }
    // 处理点击子列表项
    function ShowSonList(SonList) {
        ShowSonListUrl.value = SonList.Url;  // 更新子列表 URL
        GetTextFromUrl(SonList.Url);  // 获取子列表数据
        ShowSonListName.value = SonList.Name
    }

    // 组件挂载后获取主列表
    onMounted(() => {
        GetTextFromUrl(MainListUrl);
    });

    // 处理接收到的消息
    function receivedMessage(eventData) {
        if (eventData.startsWith("GetTextFronUrl:")) {
            const jsonString = eventData.slice("GetTextFronUrl:".length);
            let jsonData;

            try {
                jsonData = JSON.parse(jsonString);

                // 处理主列表数据
                if (jsonData.Url === MainListUrl) {

                    // 解析 ans 字符串为 JSON
                    const ansData = JSON.parse(jsonData.Ans);
                    if (Array.isArray(ansData)) {
                        MainListJson.value = ansData;  // 将主列表数据赋值
                    } else {
                        console.warn("Main List ans 解析后不是数组:", ansData);
                        MainListJson.value = [];
                    }
                }
                // 处理子列表数据
                else if (jsonData.Url === ShowSonListUrl.value) {
                    const childAnsData = JSON.parse(jsonData.Ans); // 解析子列表的 ans 字符串
                    ShowSonListJson.value = Array.isArray(childAnsData) ? childAnsData : [];
                }

            } catch (error) {
                console.error("解析 JSON 失败:", error);
            }
        }
    }

</script>
