<template>
  <div class="target-manager">
    <el-card>
      <template #header>
        <div class="header">
          <span>连接目标管理</span>
          <el-button type="primary" size="small" @click="handleAdd">
            <el-icon><IEpPlus /></el-icon>添加目标
          </el-button>
        </div>
      </template>

      <el-table :data="targets" style="width: 100%">
        <el-table-column prop="name" label="目标名称" width="200" />
        <el-table-column prop="address" label="服务器地址" />
        <el-table-column label="" width="120">
          <template #default="{ row }">
            <el-tag
              :type="isDefault(row.name) ? 'success' : 'info'"
              size="small"
              @click="setDefault(row.name)"
            >
              {{ isDefault(row.name) ? '已选中' : '选择' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150">
          <template #default="{ row }">
            <el-button size="small" @click="handleEdit(row)">编辑</el-button>
            <el-button
              size="small"
              type="danger"
              @click="handleDelete(row.name)"
              :disabled="isDefault(row.name)"
            >
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

<br />
      <div class="right-align">
        <el-button type="success" @click="connect">连接</el-button>
      </div>




    </el-card>


<!-- 编辑框  -->
      <el-dialog
      v-model="EditDialogVisible"
      :title="dialogTitle"
      width="500px"
    >
      <el-form :model="form" :rules="rules" ref="formRef">
        <el-form-item label="目标名称" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="服务器地址" prop="address">
          <el-input v-model="form.address" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="EditDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="submitForm">确认</el-button>
      </template>
    </el-dialog>


  <!--连接框-->
    <el-dialog
    v-model="ConnectionDialogVisible"
    :title="ConnectionDialogTitle"
    width="1000"
    align-center
    center
    :close-on-click-modal="false"

  >

  <el-container v-loading="!websocketStore.isConnected">
      <el-main>
        <VerifyModule @SubmitPwd="SubmitPwd" :PwdStatus="PwdStatus" />
      </el-main>
    </el-container>



  </el-dialog>



  </div>
</template>

<script setup>
import { ref, computed , onMounted , onUnmounted } from 'vue'
import { useDataStore } from '../stores/dataStore.js'
import { useWebSocketStore } from '@/stores/websocketStore.js'
import { useRouter } from 'vue-router'

import { safeJsonParse } from '@/composables/useSafeJsonParse.js'





import VerifyModule from './VerifyModule.vue'

const websocketStore = useWebSocketStore()
const store = useDataStore()
const router = useRouter()


onMounted(() => {

  const handler = (msg) =>
  {
    const msgJson = safeJsonParse(msg)
      if (msgJson.Operation === 'ivReceived' || msgJson.Operation === 'PwdNotSet')
      {
          PwdStatus.value = msgJson.Operation === 'ivReceived'
      }
      else if (msgJson.Operation === "AESCouldNotDecrypt")
      {
      store.Log("发出的消息无法解密")

        ElNotification({
          title: '密码错误',
          type: 'error',
        })
          store.SavePwd(null,'','');
      }
      else if (msgJson.Operation === "Verified")
      {
        websocketStore.Verified = true
        if(websocketStore.RemenberPwd == true ) store.SavePwd(null, websocketStore.AES_key, websocketStore.HmacKey)

        router.push('/dashboard')
      }

  }

  websocketStore.registerMessageHandler(handler) // 将处理器注册到WebSocket Store
  const handlerRef = { handler }  // 保存处理器引用，以便卸载时注销

  onUnmounted(() => {
    websocketStore.unregisterMessageHandler(handlerRef.handler)  // 组件卸载时注销处理器
  })
})


    const EditDialogVisible = ref(false)
    const form = ref({ name: '', address: '' })
    const formRef = ref(null)
    const isEditing = ref(false)
    const editingName = ref('')

    const ConnectionDialogVisible = ref(false)
    const ConnectionDialogTitle = computed(() =>
    {
  return `连接到${DefTarget.value.name} ( ${DefTarget.value.address} )...`;
    })
    const PwdStatus = ref(null);

    const targets = computed(() => store.ConnectTargets)
    const defaultTarget = computed(() => store.DefTarget)
  const DefTarget = computed(() => store.getDefaultTarget())

  function connect()
  {
  websocketStore.close()
  websocketStore.connect(DefTarget.value)
  ConnectionDialogVisible.value = true
  }

  function SubmitPwd(pwd, Remenber)
  {
    if(PwdStatus.value) websocketStore.VerifyPwd(pwd,Remenber)
    else websocketStore.SetPwd(pwd)
  }




    const dialogTitle = computed(() => isEditing.value ? '编辑目标' : '添加目标')

    const rules = {
      name: [
        { required: true, message: '请输入目标名称', trigger: 'blur' },
        {
          validator: (_, value, callback) => {
            const exists = targets.value.some(
              t => t.name === value && (!isEditing.value || t.name !== editingName.value)
            )
            exists ? callback(new Error('名称已存在')) : callback()
          },
          trigger: 'blur'
        }
      ],
      address: [
        { required: true, message: '请输入服务器地址', trigger: 'blur' },
        {
          validator: (_, value, callback) => {
            value.startsWith('ws://') || value.startsWith('wss://') || value == "[visitTarget]"
              ? callback()
              : callback(new Error('地址必须以ws://或wss://开头,或者使用[visitTarget]指向当前访问的地址'))
          },
          trigger: 'blur'
        }
      ]
    }

    const isDefault = (name) => defaultTarget.value === name

    // const getDefaultAddress = () => {
    //   const target = targets.value.find(t => t.name === defaultTarget.value)
    //   return target?.address || ''
    // }

    const handleAdd = () => {
      isEditing.value = false
      form.value = { name: '', address: '' }
      EditDialogVisible.value = true
    }

    const handleEdit = (target) => {
      isEditing.value = true
      editingName.value = target.name
      form.value = { ...target }
      EditDialogVisible.value = true
    }

    const handleDelete = async (name) => {
      try {
        await ElMessageBox.confirm(`确定删除目标 "${name}"?`, '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        })
        store.removeTarget(name)
        ElMessage.success('删除成功')
      } catch {
        // 取消操作
      }
    }

    const setDefault = (name) => {
      try {
        store.setDefaultTarget(name)
        ElMessage.success('默认目标已更新')
      } catch (error) {
        ElMessage.error(error.message)
      }
    }

    const submitForm = async () => {
      try {
        await formRef.value.validate()

        if (isEditing.value) {
          store.updateTarget(editingName.value, form.value.name, form.value.address)
          ElMessage.success('更新成功')
        } else {
          store.addTarget(form.value.name, form.value.address)
          ElMessage.success('添加成功')
        }

        EditDialogVisible.value = false
      } catch (error) {
        if (error.message) {
          ElMessage.error(error.message)
        }
      }
    }

</script>

<style scoped>
.right-align {
  margin-left: auto;
  margin-right: 0;
  width: fit-content; /* 或指定固定宽度 */
}

.target-manager {
  max-width: 900px;
  margin: 0 auto;
  padding: 20px;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.current-default {
  margin-top: 20px;
  padding: 10px;
  background-color: #f5f7fa;
  border-radius: 4px;
}

.current-default p {
  margin: 5px 0;
}
</style>
