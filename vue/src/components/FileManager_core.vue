<template>
  <h2>文件管理</h2>


  <div style="margin: 10px 0">
    <el-button type="primary" plain circle @click="SelectMode = (SelectMode == 'single') ? 'multiple' : 'single'">
      <el-icon v-auto-animate>
        <IEpDocumentChecked v-if="SelectMode == 'single'" />
        <IEpFinished v-else />
      </el-icon>
    </el-button>



    <el-button color="#626aef" plain @click="showUploadPage = true">添加</el-button>

    <el-button color="#626aef" plain
               @click="Download()"
               :disabled="(SelectMode == 'single' && Selected == null) || (SelectMode == 'multiple' && Selected_multiple.length == 0)">
      下载
    </el-button>

    <el-button color="#FF5722" plain
               @click="CopySelected()"
               :disabled="(SelectMode == 'single' && Selected == null) || (SelectMode == 'multiple' && Selected_multiple.length == 0)">
      复制
    </el-button>

    <el-popover :visible="showClipBoard"
                placement="bottom"
                :width="300">
      <template #reference>
        <el-button color="#FF5722" plain @click="showClipBoard = !showClipBoard">
          剪切板
        </el-button>
      </template>

      <div v-auto-animate>
        <el-button v-if="Copied.length != 0" type="primary" @click="Paste(false)">粘贴到此目录</el-button>
        <el-button v-if="Copied.length != 0" type="primary" @click="Paste(true)">剪切到此目录</el-button>
        <el-button v-if="Copied.length != 0" type="primary" @click="Copied.length = 0">清空</el-button>

        <el-empty v-else description="还没有内容" />

        <br />
        <el-text v-for="file in Copied">
          {{file}}
          <hr />
        </el-text>
      </div>
    </el-popover>

    <el-button type="warning" plain
               @click="EditName"
               :disabled="SelectMode != 'single' || Selected == null">
      重命名
    </el-button>



    <el-popconfirm class="ml-5"
                   confirm-button-text='确定'
                   cancel-button-text='我再想想'
                   icon="el-icon-info"
                   icon-color="red"
                   title="确定删除选定的内容吗？"
                   @confirm="Delete">
      <template #reference>
        <el-button type="danger" plain
                   :disabled="(SelectMode == 'single' && Selected == null) || (SelectMode == 'multiple' && Selected_multiple.length == 0)"
                   slot="reference">删除 <i class="el-icon-remove-outline"></i></el-button>
      </template>
    </el-popconfirm>


    <el-popover :width="300">
      <template #reference>
        <el-button color="#66CC99" plain
                   :disabled="(SelectMode == 'single' && Selected == null) || (SelectMode == 'multiple' && Selected_multiple.length == 0)">
          压缩
        </el-button>
      </template>
      <el-text>压缩文件名称</el-text><br /><br />
      <el-input v-model="ZipName" style="width: 200px" /><br />
      <el-button color="#626aef" @click="ZipFile">提交</el-button>
    </el-popover>

  </div>

  <el-dialog v-model="showUploadPage"
             title="添加文件"
             width="500"
             align-center>

    <el-tabs>

      <el-tab-pane label="新建空文件/文件夹">
        <el-text>名称</el-text><br />
        <el-input v-model="newItemName" style="width: 300px" /><br />

        <el-radio-group v-model="newItemType">
          <el-radio value="file" size="large">文件</el-radio>
          <el-radio value="dir" size="large">文件夹</el-radio>
        </el-radio-group>
        <br />
        <el-button color="#626aef" @click="NewItem">提交</el-button>
      </el-tab-pane>

      <el-tab-pane label="上传">
        <el-upload v-show="showUploadPage"
                   ref="uploadRef"
                   drag
                   :action="uploadUrl"
                   :auto-upload="false"
                   multiple>
          <el-icon class="el-icon--upload"><IEpupload-filled /></el-icon>
          <div class="el-upload__text">
            拖拽文件到此处 或 <em>点击选取文件</em>
          </div>

          <template #tip>
            <div class="el-upload__tip">
              提交任务后可以关闭上传窗口，但是不要在任务完成前离开文件管理页
            </div>
            <el-button class="ml-3" type="success" @click="submitUpload">
              上传
            </el-button>
          </template>
        </el-upload>
      </el-tab-pane>

      <el-tab-pane label="从url下载">
        <el-text>文件名称</el-text><br />
        <el-input v-model="DownloadFileName" style="width: 300px" />
        <br /><br />
        <el-text>目标url</el-text><br />
        <el-input v-model="DownloadUrl" style="width: 300px" /><br /><br />
        <el-button color="#626aef" @click="NewItem_FromUrl">提交</el-button>
      </el-tab-pane>

    </el-tabs>
  </el-dialog>


  <el-dialog v-model="Opened"
             :title="OpenItem.Name"
             width="500"
             align-center>

    <div>
      <el-button color="#66CC99" plain @click="RunOpened">
        运行
      </el-button>

      <el-button color="#66CC99" plain @click="unZipFile"
                 v-if="OpenItem.Type == 'zip'">
        解压
      </el-button>

    </div>


  </el-dialog>






  <el-breadcrumb separator="/" v-if="!EditPath">
    <el-breadcrumb-item @click="ChangePath('')" class="clickable-breadcrumb">此电脑</el-breadcrumb-item>
    <el-breadcrumb-item v-if="Path.length == 0">
      根目录
    </el-breadcrumb-item>

    <el-breadcrumb-item v-for="(item, index) in Path"
                        :key="index"
                        @click="ChangePathByNum(index)">
      {{ item }}
    </el-breadcrumb-item>

    <el-breadcrumb-item>
      <el-icon><IEpEditPen @click=" EditPathValue = RawPath ; EditPath = true" /></el-icon>
    </el-breadcrumb-item>

  </el-breadcrumb>

  <div v-else>
    <el-input v-model="EditPathValue" style="width: 300px" />

    <el-button type="danger" plain circle @click=" EditPath = false">
      <el-icon><IEpCloseBold /></el-icon>
    </el-button>
    <el-button type="success" plain circle @click="ChangePath(EditPathValue)">
      <el-icon><IEpSelect /></el-icon>
    </el-button>
  </div>




  <el-row v-auto-animate>
    <el-col :span="1" v-if="SelectMode == 'multiple'">

      <el-checkbox v-model="checkAll"
                   :indeterminate="isIndeterminate"
                   @change="handleCheckAllChange"
                   style="height: 30px;">

      </el-checkbox>

      <el-checkbox-group v-model="Selected_multiple"
                         @change="handleSelectedChange">
        <div v-for="(item, index) in Content" style="height: 30px;" class="">
          <el-checkbox label="" :value="index" />

        </div>


      </el-checkbox-group>

    </el-col>
    <el-col :span="23" v-auto-animate>

      <el-row style="height: 30px;">
        <el-col :span="10"><div class="grid-content ep-bg-purple-light" />文件名</el-col>
        <el-col :span="4"><div class="grid-content ep-bg-purple-light" />类型</el-col>
        <el-col :span="4"><div class="grid-content ep-bg-purple" />大小</el-col>
        <el-col :span="6"><div class="grid-content ep-bg-purple-light" />修改时间</el-col>

      </el-row>

      <div v-for="(item, index) in Content"
           :class="{'transition': true}"
           :style="(index == Selected) ? 'background-color: #D8EBFF;' : ''">

        <el-row @click="Selected = index" @dblclick="dbClick(index)">
          <el-col :span="10" v-auto-animate>
            <div class="grid-content ep-bg-purple-light" />
            <div v-if="RenameNum == index">
              <el-input v-model="newName" style="width: 60%" />
              <el-button type="danger" plain circle @click="CancelRename">
                <el-icon><IEpCloseBold /></el-icon>
              </el-button>
              <el-button type="success" plain circle @click="Rename">
                <el-icon><IEpSelect /></el-icon>
              </el-button>
            </div>

            <el-text truncated v-else>{{item.Name}}</el-text>

          </el-col>

          <el-col :span="4">
            <div class="grid-content ep-bg-purple-light" />
            <el-text truncated>{{item.showType}}</el-text>
          </el-col>

          <el-col :span="4">
            <div class="grid-content ep-bg-purple" />
            <el-text truncated>{{item.Size}}</el-text>
          </el-col>

          <el-col :span="6">
            <div class="grid-content ep-bg-purple-light" />
            <el-text truncated>{{item.Time}}</el-text>
          </el-col>

        </el-row>
        <hr />
      </div>
    </el-col>

  </el-row>


</template>

<script setup>
  import { ref, onMounted, onUnmounted } from 'vue';
  import { useWebSocketStore } from '@/stores/websocketStore';

  const websocketStore = useWebSocketStore()

  const wsUrl = websocketStore.Target.address

  const SendMsg = websocketStore.sendMessage


  const Path = ref([]);
  const Content = ref([]);
  const RawPath = ref("");
  const EditPath = ref(false);
  const EditPathValue = ref("");


  const Selected = ref(null);
  const Selected_multiple = ref([]);
  const SelectMode = ref("single");
  const checkAll = ref(false)
  const isIndeterminate = ref(false)

  const showUploadPage = ref(false)
  const DownloadUrl = ref("")
  const DownloadFileName = ref("")

  const OpenItem = ref({})
  const Opened = ref(false)

  const RenameNum = ref(-1);
  const newName = ref("");

  const newItemName = ref("");
  const newItemType = ref("file");

  const ZipName = ref("")

  const uploadRef = ref()
  const uploadUrl = ref("http://127.0.0.1:7799/upload")

    function RunActions(ActionCalls) {
      const reqObj = {
        Operation: "RunAction",
        Action: ActionCalls
      };
      const reqJson = JSON.stringify(reqObj);
      SendMsg(reqJson)
    }


  function submitUpload() {
    const reqObj = {
      Operation: "FileManager",
      op: "Upload",
      path: RawPath.value
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  const handleCheckAllChange = (val) => {
    const SelectedAllArray = [];

    for (let i = 0; i < Content.value.length; i++) {
      SelectedAllArray.push(i);
    }

    Selected_multiple.value = val ? SelectedAllArray : []
    isIndeterminate.value = false
  }

  const handleSelectedChange = (value) => {
    const checkedCount = value.length
    checkAll.value = checkedCount === Content.value.length
    isIndeterminate.value = checkedCount > 0 && checkedCount < Content.value.length
  }

  function Download() {
    const downloadList = GetSelectedFilePath()
    const reqObj = {
      Operation: "FileManager",
      op: "Download",
      path: downloadList
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function NewItem()
  {
    const reqObj = {
      Operation: "FileManager",
      op: "NewItem",
      path: RawPath.value + newItemName.value,
      type: newItemType.value
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function NewItem_FromUrl() {

    if (Path.value.length == 0) {
      ElNotification({
        title: "不能在目录页下载",
        type: "error",
      })
      return
    }
    const downloadPath = RawPath.value + DownloadFileName.value
    const ActionCalls = [{
        ActionName: "download",
      Paras: [{ ParaName: "url", value: DownloadUrl.value }, { ParaName: "path", value: downloadPath }]
    }]
    RunActions(ActionCalls)

    DownloadUrl.value = ""

    ElNotification({
      title: "已提交下载任务",
      type: "success",
    })

  }

  const Copied = ref([]);
  const showClipBoard = ref(false)

  function CopySelected() {
    if (Path.value.length == 0) {
      ElNotification({
        title: "根目录下内容不能复制",
        message: "",
        type: "error",
      })
      return;
    }
      Copied.value = GetSelectedFilePath()
    ElNotification({
      title: "已复制" + Copied.value.length + "个内容",
      message: "",
      type: "success",
    })
  }

  function Paste(cut) {
    if (Path.value.length == 0) {
      ElNotification({
        title: "根目录无法写入",
        message: "",
        type: "error",
      })
      return;
    }
    const reqObj = {
      Operation: "FileManager",
      op: "Paste",
      resPath: Copied.value,
      targetPath: RawPath.value,
      cut: cut
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function EditName() {
    if (SelectMode.value == "multiple") return;
    newName.value = Content.value[Selected.value].Name;
    RenameNum.value = Selected.value;
  }

  function CancelRename() {
    RenameNum.value = -1;
    newName.value = "";
  }

  function Rename() {
    const FilePath = RawPath.value + Content.value[Selected.value].Name;
    const reqObj = {
      Operation: "FileManager",
      op: "Rename",
      path: FilePath,
      newName: newName.value
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
    CancelRename()
  }

  function Delete() {
    const DeleteList = GetSelectedFilePath()
    const reqObj = {
      Operation: "FileManager",
      op: "Delete",
      DeleteList: DeleteList
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function RunOpened() {
    if (!Opened.value) return;

    const command = "\"" + RawPath.value + OpenItem.value.Name + "\"";
    const ActionCalls = [{
      ActionName: "cmd",
      Paras: [{ ParaName: "command", value: command }]
    }]
    RunActions(ActionCalls)

    ElNotification({
      title: "已发送运行任务",
      message: "",
      type: "success",
    })
  }

  function ZipFile() {
    if (!ZipName.value.endsWith(".zip")) ZipName.value += ".zip"
    const ZipPath = RawPath.value + ZipName.value;

    const ZipList = GetSelectedFilePath()
    const reqObj = {
      Operation: "FileManager",
      op: "ZipFile",
      ZipList: ZipList,
      targetPath: ZipPath
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function unZipFile() {
    const zipFilePath = RawPath.value + OpenItem.value.Name
    const psContent = `Expand-Archive -Path "${zipFilePath}" -DestinationPath (Split-Path "${zipFilePath}")`;
    const ActionCalls = [{
      ActionName: "RunPs",
      Paras: [{ ParaName: "psContent", value: psContent }]
    }]
    RunActions(ActionCalls)

    ElNotification({
      title: "已发送解压任务",
      message: "",
      type: "success",
    })
  }

  function GetSelectedFilePath() {
    var AnsList = []
    if (SelectMode.value == "single") {
      const ResPath = RawPath.value + Content.value[Selected.value].Name
      AnsList.push(ResPath)
    }
    else {
      Selected_multiple.value.forEach(itemNum => {
        const ResPath = RawPath.value + Content.value[itemNum].Name
        AnsList.push(ResPath)
      });
    }
    return AnsList
  }

  function ChangePath(path) {
    const reqObj = {
      Operation: "FileManager",
      op: "GetContent",
      path: path+"\\"
    };
    const reqJson = JSON.stringify(reqObj);
    SendMsg(reqJson)
  }

  function ChangePathByNum(num) {
      const newPath = Path.value.slice(0, num + 1).join('\\');      // 通过 num 获取路径，拼接成为字符串
      ChangePath(newPath);
    }

    function dbClick(index)
    {
      const item = Content.value[index]
      if(item.Type == "dir")
      {
        var newPath = RawPath.value + item.Name;
        ChangePath(newPath)
      }
      else if(item.Type == "lnk" || item.Type == "/disk" )
      {
        ChangePath(item.Lnk)
      }
      else
      {
        OpenItem.value = Content.value[index];
        Opened.value = true
      }

    }

  onMounted(() => {

  });

  onMounted(() => {
  const handler = (data) =>
  {
      receivedMessage(data);
  }  // 注册特定类型消息的处理

  websocketStore.registerMessageHandler(handler) // 将处理器注册到WebSocket Store
  const handlerRef = { handler }  // 保存处理器引用，以便卸载时注销

  onUnmounted(() => {
    websocketStore.unregisterMessageHandler(handlerRef.handler)  // 组件卸载时注销处理器
  })

  ChangePath("")
})


  function receivedMessage(eventData) {
    if (eventData.startsWith("FileContent:")) {
      const jsonString = eventData.slice("FileContent:".length);
      const jsonData = JSON.parse(jsonString);

      var fileList = jsonData.Content;

      fileList.forEach(file => {
        // 查找对应的中文名称
        if (knownFileTypes[file.Type]) {
          file.showType = knownFileTypes[file.Type];
        } else {
          file.showType = file.Type; // 如果类型未定义，设置为未知类型
        }
      });


      Path.value = jsonData.Path;
      Content.value = jsonData.Content;

      RawPath.value = Path.value.slice(0, Path.value.length).join('\\') + "\\"

      Selected.value = null;
      Selected_multiple.value = [];

      CancelRename()
      EditPathValue.value = RawPath.value

    }
    else if (eventData.startsWith("RefreshFileContent:")) {
      ChangePath(RawPath.value)
    }

    else if (eventData.startsWith("FileDownloadToken:")) {
      const token = eventData.slice("FileDownloadToken:".length);

      var httpUrl = "http" + wsUrl.substring(2);
      if(!httpUrl.endsWith("/")) httpUrl += "/"
      if(wsUrl == "[visitTarget]") httpUrl = window.location.href
      var fullUrl = `${httpUrl}download?token=${token}`;

      window.open(fullUrl, "_blank")

      ElNotification({
          title: "下载已开始",
          message: `多个文件可能需要一定时间来打包，如果长时间按未开始下载，可以尝试访问${fullUrl}`,
          type: "success",
        })

    }

    else if (eventData.startsWith("FileUploadToken:")) {
      const token = eventData.slice("FileUploadToken:".length);
      const connecturl = websocketStore.Target.address
    .replace(/^wss:\/\//, 'https://')
    .replace(/^ws:\/\//, 'http://');

      uploadUrl.value = `${connecturl}/upload?token=${token}`;

      uploadRef.value.submit()
      ElNotification({
        title: "上传成功",
        message: "",
        type: "success",
      })
      ChangePath(RawPath.value)
    }

  }

  const knownFileTypes = {
    "txt": "文本文件",
    "jpg": "图片文件",
    "png": "图片文件",
    "gif": "动画图片",
    "pdf": "PDF文件",
    "doc": "Word文档",
    "xls": "Excel表格",
    "dir": "文件夹",
    "bat": "Windows批处理文件",
    "/disk": "磁盘",
    "tmp": "临时文件",
    "json": "json数据文件",
    "sys": "系统文件",
    "dll": "应用程序拓展",
    "exe": "可执行文件",
    "lnk": "快捷方式",
    "html": "网页文件",
    "bmp": "图片文件",
    "zip": "压缩文件",
    "db": "数据库文件"
      // 可以继续添加其他文件类型
    };



</script>

<style scoped>
  /* 添加过渡效果 */
  .transition {
    transition: background-color 0.3s ease;
    height: 30px;
  }
  .clickable-breadcrumb {
  cursor: pointer;
  color: #409eff; /* Element Plus 主色 */
}
.clickable-breadcrumb:hover {
  color: #66b1ff;
}
</style>
