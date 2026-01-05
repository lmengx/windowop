<template>
<el-menu
    :default-active="index"
    mode="horizontal"
    class="light-menu"
  >
    <el-menu-item index="1" @click="RouterPush('/dashboard/scriptmanager/targetlist')">自动化脚本</el-menu-item>
    <el-menu-item index="2" @click="RouterPush('/dashboard/scriptmanager/pannel')">执行脚本</el-menu-item>
  </el-menu>


<router-view />
</template>

<script setup>
  import { computed } from 'vue'
  import { useRouter } from 'vue-router'
  import { useRoute } from 'vue-router'

  const router = useRouter()
  const route = useRoute()

function RouterPush(target)
{
  router.push(target)
}

    const index = computed(() =>
    {
        if(route.path == "/dashboard/scriptmanager/targetlist") return "1";
        return "2";
    })


</script>
