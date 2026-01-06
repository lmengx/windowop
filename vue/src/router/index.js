import { createRouter, createWebHistory } from 'vue-router'
  import HomeView from '../components/HomeView.vue'
  import Connect from '../components/Connect.vue'
  import Dashboard from '../components/Dashboard.vue'
      import MachineStatus from '../components/MachineStatus.vue';
      import RemoteDesk from '../components/RemoteDesk.vue';
      import ScriptManager from '@/components/ScriptManager.vue';
        import TargetList from '../components/Target_List.vue';
        import Pannel from '@/components/Pannel.vue';
      import FileManager_core from '@/components/FileManager_core.vue'
      import Setting from '@/components/Setting.vue';



const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'home',
      component: HomeView,
    },
    {
      path: '/connect',
      name: 'connect',
      component: Connect,
    },
    {
      path: '/dashboard',
      name: 'dashboard',
      component: Dashboard,
      children: [
        {
          path: '',
          name: 'machineStatus',
          component: MachineStatus // 进入 /user 时默认显示这个
        },
        {
          path: 'remotedesk',
          name: 'remotedesk',
          component: RemoteDesk
        },
        {
          path: 'scriptmanager',
          name: 'scriptmanager',
          component: ScriptManager,
            children: [
                        {
                          path: 'targetlist',
                          name: 'targetlist',
                          component: TargetList
                        },
                        {
                          path: 'pannel',
                          name: 'pannel',
                          component: Pannel
                        },
                      ]
        },
        {
          path: 'filemanager',
          name: 'filemanager',
          component: FileManager_core
        },
        {
          path: 'setting',
          name: 'setting',
          component: Setting
        },

      ]
    },
  ],
})

export default router
