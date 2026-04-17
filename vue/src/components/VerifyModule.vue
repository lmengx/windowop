

<template>
  <div class="verify-container">
    <div class="verify-content">
      <h3 class="title" v-if="props.PwdStatus">验证密码</h3>
      <h3 class="title" v-else>设置密码</h3>

      <el-input 
        v-model="Input" 
        show-password 
        placeholder="请输入密码"
        class="pwd-input"
        @keydown.enter="SubmitPwd" 
      />
      
      <div class="remember-box" v-if="props.PwdStatus">
        <el-checkbox v-model="Remenber" label="记住密码" />
      </div>
      
      <el-button type="primary" class="submit-btn" @click="SubmitPwd">
        提交
      </el-button>
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
.verify-container {
  display: flex;
  justify-content: center;
  align-items: center;
  padding: 20px 0;
}

.verify-content {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  max-width: 280px;
}

.title {
  margin: 0 0 20px 0;
  color: #303133;
  font-size: 18px;
  font-weight: 500;
}

.pwd-input {
  width: 100%;
  margin-bottom: 16px;
}

.pwd-input :deep(.el-input) {
  width: 100%;
}

.pwd-input :deep(.el-input__wrapper) {
  padding: 10px 14px;
  width: 100%;
  box-sizing: border-box;
}

/* 隐藏显示密码图标，防止它影响布局 */
.pwd-input :deep(.el-input__suffix) {
  position: absolute;
  right: 10px;
  pointer-events: none;
}

.pwd-input :deep(.el-input__suffix-inner) {
  pointer-events: none;
}

.remember-box {
  margin-bottom: 20px;
  width: 100%;
  text-align: center;
}

.submit-btn {
  width: 100%;
}
</style>
