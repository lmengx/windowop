<template>
  <el-radio-group v-model="isCollapse" style="margin-bottom: 20px">
    <el-radio-button :value="false">expand</el-radio-button>
    <el-radio-button :value="true">collapse</el-radio-button>
  </el-radio-group>
  <el-menu
    default-active="2"
    class=""
    :collapse="isCollapse"
    @open=""
    @close=""
  >
    <el-menu-item index="2">
      <el-icon><icon-menu /></el-icon>
      <template #title>测试</template>
    </el-menu-item>
    <el-menu-item index="3" disabled>
      <el-icon><document /></el-icon>
      <template #title>Navigator Three</template>
    </el-menu-item>

  </el-menu>
</template>

<script setup>


const isCollapse = ref(true)

</script>

<style scoped>

</style>
