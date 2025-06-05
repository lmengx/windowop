<template>
  <div class="fullscreen centerbox" v-show="!showEula">
    <div class="pwdbox">
      <br />
      <el-text size="large">设置密码</el-text>

      <br>
      <el-input v-model="Input" show-password style="width: 180px" @keydown.enter="SetPassword" />
      <br /><br />
      <el-button type="primary" class="submit" @click="SetPassword">提交</el-button>
    </div>
  </div>

  <el-dialog v-model="showEula"
             title="使用须知"
             width="500"
             :before-close="()=>{}"
             :show-close="false"
             :close-on-press-escape="false"
             :close-on-click-modal="false">
    <ul>
      <li>
        <el-text>本应用禁止用于违法用途，如破坏、监控他人设备,使用者的非法行为与作者无关</el-text>
      </li>
      <li>
        <el-text>本应用开源，请保证你获取此应用的站点的可靠性</el-text>
      </li>
      <li>
        <el-link type="primary" @click="showEulaDetail = true">功能注意事项</el-link>
      </li>
    </ul>

    <el-dialog v-model="showEulaDetail"
               width="500"
               title="注意事项"
               append-to-body>

      <el-text size="large">文件管理功能</el-text>
      <ul>
        <li>
          <el-text>文件管理的上传与下载为明文传输，不要传输重要的文件</el-text>
        </li>
      </ul>

      <el-text size="large">保护设置</el-text>
      <ul>
        <li>
          <el-text>开启保护设置后忘记密码，导致的无法卸载，后果自负</el-text>
        </li>
      </ul>

      <el-text size="large">测试功能--全前缀监听</el-text>
      <ul>
        <li>
          <el-text>会使得其他设备可以访问控制网页，本应用的加密不能防止刻意针对本应用的中间人攻击，安全性相当于没有CA的SSL加密，不要在重要的机器上开启此功能</el-text>
        </li>
      </ul>

    </el-dialog>

    <template #footer>
      <div class="dialog-footer">
        <el-checkbox v-model="agreeEula" label="我已阅读并同意上述内容" size="large" />
        <br />
        <el-button type="primary" :disabled="!agreeEula" @click="showEula = false">
          开始使用
        </el-button>
      </div>
    </template>

  </el-dialog>


</template>

<script setup>

  import { ref, inject } from 'vue';

  const showEula = ref(true)
  const showEulaDetail = ref(false)

  const agreeEula = ref(false);

  const emit = defineEmits(['SetPwd'])

    const Input = ref('')
    const SendMsg = inject("provideFuncSendWSMsg")


  function SetPassword() {
      emit("SetPwd", Input.value)        
    }
</script>
<style scoped>
  .pwdbox {
    width: 300px;
    height: 170px;
    text-align: center; /* 文本居中 */
    border: 1px solid #a1a1a1;
    border-radius: 5px;
  }

  .submit {
    width: 180px;
  }
</style>
