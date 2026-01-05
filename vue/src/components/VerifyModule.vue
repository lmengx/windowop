

<template>
  <div>
    <div class="pwdbox">
      <br />
      <el-text  v-if="props.PwdStatus" size="large">验证密码</el-text>
      <el-text  v-else size="large">设置密码</el-text>


      <br>
      <el-input v-model="Input" show-password style="width: 180px" @keydown.enter="SubmitPwd" />
      <br />
      <el-checkbox v-if="props.PwdStatus" v-model="Remenber" label="记住密码" size="large" />
      <br />
      <el-button type="primary" class="submit" @click="SubmitPwd">提交</el-button>
    </div>
  </div>



</template>

<script setup>
import { ref } from 'vue'


  const props = defineProps(['PwdStatus'])


  const emit = defineEmits(['SubmitPwd'])

  function SubmitPwd() {
      emit("SubmitPwd", Input.value, Remenber.value)
  }


  const Input = ref('')
  const Remenber = ref(false)

</script>

<style scoped>
  .pwdbox{
    width: 300px;
    height: 170px;
    text-align: center; /* 文本居中 */
    border:1px solid #a1a1a1;
    border-radius:5px;

  }

  .submit{
    width: 180px;
  }

</style>
