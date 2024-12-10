<template>

    <el-container class="common-layout">
      <el-aside width="100px" class="">
              <p>windowOP</p>
      <button class="btn" @click="() => showPage = 'TargetList'">检测规则</button>
      <button class="btn" @click="() => showPage = 'Pannel'">控制面板</button>
      <button class="btn" @click="() => showPage = 'Setting'">设置</button>
      </el-aside>
      <el-main>
        <TargetList 
                  v-if="showPage == 'TargetList'"/>

      <Pannel v-if="showPage == 'Pannel'" />

      <Setting v-if="showPage == 'Setting'" />
      </el-main>
    </el-container>

</template>

<script setup>

    import { ref, provide, inject } from 'vue';
    import TargetList from './Target_List.vue';
    import Pannel from './Pannel.vue';
    import Setting from './Setting.vue';



    const SendMsg = inject("provideFuncSendWSMsg")
    provide("provideFuncRunActions",  RunActions)
    provide("provideFuncRunActionNoPara", RunActionNoPara)

    function RunActions(ActionCalls) {
        const ActionStr = JSON.stringify(ActionCalls, null, 2);
        const reqJson = `{"Operation": "RunAction",
                            "Action": ${ActionStr}
                        }`;
        SendMsg(reqJson)
    }
    function RunActionNoPara(ActionName)
    {
        const ActionCalls = `[
            {
                "ActionName": "${ActionName}",
                "Paras": []
            }
        ]`;
        RunActions(ActionCalls)
    }




    const showPage = ref("TargetList");



</script>

<style scoped>

</style>
