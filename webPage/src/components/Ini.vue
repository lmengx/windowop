<template>
  <div>
    <div class="pwdbox">
      <br />
      <el-text size="large">设置密码</el-text>

      <br>
      <el-input v-model="Input" show-password style="width: 180px" @keydown.enter="SetPassword" />
      <br /><br />
      <el-button type="primary" class="submit" @click="SetPassword">提交</el-button>
    </div>
  </div>


</template>

<script setup>

    import { ref, inject } from 'vue';

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
