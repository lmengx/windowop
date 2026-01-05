<template>
  <div class="machine-monitor">
    <h2>机器资源监控</h2>

    <!-- 第一排：CPU & 内存 -->
    <div :class="['chart-row', { 'mobile-column': isMobile }]">
      <div ref="cpuChartRef" class="chart-item"></div>
      <div ref="memoryChartRef" class="chart-item"></div>
    </div>

    <!-- 磁盘区域 -->
    <div v-for="drive in Object.keys(history.disks)" :key="drive" class="disk-row">
      <h3>{{ drive }} 磁盘</h3>
      <div :class="['chart-row', { 'mobile-column': isMobile }]">
        <div :ref="(el) => (diskPieRefs[drive] = el)" class="disk-pie"></div>
        <div :ref="(el) => (diskLineRefs[drive] = el)" class="disk-line"></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, reactive, nextTick } from 'vue'
import * as echarts from 'echarts'
import { useWebSocketStore } from '@/stores/websocketStore.js'
import { safeJsonParse } from '@/composables/useSafeJsonParse'

import { useWindowWidth } from '@/composables/useWindowWidth'
const { windowWidth } = useWindowWidth()
const isMobile = computed(() => windowWidth.value < 960)

const websocketStore = useWebSocketStore()

// 图表 DOM 引用
const cpuChartRef = ref(null)
const memoryChartRef = ref(null)
const diskPieRefs = ref({})
const diskLineRefs = ref({})

// 图表实例（只初始化一次）
let cpuChartInstance = null
let memoryChartInstance = null
const diskPieInstances = {}
const diskLineInstances = {}

// 历史数据
const history = reactive({
  timestamps: [],
  cpu: [],
  memory: [],
  disks: {}
})

const MAX_HISTORY = 20

// 获取 CPU 图表初始配置（不含数据）
const getCpuOption = () => ({
  title: { text: 'CPU 使用率 (%)', left: 'center', top: 0 },
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: [] },
  yAxis: { type: 'value', min: 0, max: 100 },
  series: [{
    type: 'line',
    areaStyle: {},
    data: [],
    smooth: true,
    color: '#5470c6',
    showSymbol: false,
    animationDuration: 800,
    animationEasing: 'easeOutQuint'
  }]
})

// 获取内存图表初始配置
const getMemoryOption = () => ({
  title: { text: '内存使用率 (%)', left: 'center', top: 0 },
  tooltip: { trigger: 'axis' },
  xAxis: { type: 'category', data: [] },
  yAxis: { type: 'value', min: 0, max: 100 },
  series: [{
    type: 'line',
    areaStyle: {},
    data: [],
    smooth: true,
    color: '#91cc75',
    showSymbol: false,
    animationDuration: 800,
    animationEasing: 'easeOutQuint'
  }]
})

// 更新历史数据
const updateHistory = (newData) => {
  const now = newData.Timestamp
  const cpu = parseFloat(newData.CPU.UsagePercent.toFixed(1))
  const mem = parseFloat(newData.Memory.UsagePercent.toFixed(1))

  const newDiskUsages = {}
  newData.Disks.forEach(disk => {
    newDiskUsages[disk.DriveLetter] = parseFloat(disk.UsagePercent.toFixed(1))
  })

  history.timestamps.push(now)
  history.cpu.push(cpu)
  history.memory.push(mem)

  // 初始化或追加磁盘数据
  Object.keys(newDiskUsages).forEach(drive => {
    if (!history.disks[drive]) history.disks[drive] = []
    history.disks[drive].push(newDiskUsages[drive])
  })

  // 对于已存在但本次未上报的磁盘，补 NaN
  Object.keys(history.disks).forEach(drive => {
    if (!(drive in newDiskUsages)) {
      history.disks[drive].push(NaN)
    }
  })

  // 超出长度则移除最旧数据
  if (history.timestamps.length > MAX_HISTORY) {
    history.timestamps.shift()
    history.cpu.shift()
    history.memory.shift()
    Object.keys(history.disks).forEach(drive => {
      history.disks[drive].shift()
    })
  }

  nextTick(() => {
    updateAllCharts()
  })
}

// 更新所有图表（按需初始化 + setOption）
const updateAllCharts = () => {
  // --- CPU ---
  if (cpuChartRef.value && !cpuChartInstance) {
    cpuChartInstance = echarts.init(cpuChartRef.value)
    cpuChartInstance.setOption(getCpuOption(), true)
  }
  if (cpuChartInstance) {
    cpuChartInstance.setOption({
      xAxis: { data: formatTimeLabels() },
      series: [{ data: history.cpu }]
    }, { notMerge: false })
  }

  // --- 内存 ---
  if (memoryChartRef.value && !memoryChartInstance) {
    memoryChartInstance = echarts.init(memoryChartRef.value)
    memoryChartInstance.setOption(getMemoryOption(), true)
  }
  if (memoryChartInstance) {
    memoryChartInstance.setOption({
      xAxis: { data: formatTimeLabels() },
      series: [{ data: history.memory }]
    }, { notMerge: false })
  }

  // --- 磁盘 ---
  updateDiskCharts()
}

// 更新磁盘图表（饼图 + 面积图）
const updateDiskCharts = () => {
  const drives = Object.keys(history.disks)

  drives.forEach(drive => {
    const usage = history.disks[drive][history.disks[drive].length - 1] || 0
    const color = usage > 90 ? '#d94646' : '#3b82f6'

    // --- 饼图 ---
    const pieEl = diskPieRefs.value[drive]
    if (pieEl && !diskPieInstances[drive]) {
      diskPieInstances[drive] = echarts.init(pieEl)
      diskPieInstances[drive].setOption({
        tooltip: { formatter: `{b}: {d}%` },
        series: [{
          type: 'pie',
          radius: ['60%', '100%'],
          avoidLabelOverlap: false,
          label: { show: false },
          emphasis: { scale: false },
          labelLine: { show: false },
          data: [
            { value: usage, itemStyle: { color } },
            { value: 100 - usage, itemStyle: { color: '#f0f0f0' } }
          ]
        }],
        graphic: {
          elements: [{
            type: 'text',
            left: 'center',
            top: 'middle',
            style: {
              text: `${usage}%`,
              fontSize: 16,
              fontWeight: 'bold'
            }
          }]
        }
      }, true)
    }

    if (diskPieInstances[drive]) {
      diskPieInstances[drive].setOption({
        series: [{
          type: 'pie',
          data: [
            { value: usage, itemStyle: { color } },
            { value: 100 - usage, itemStyle: { color: '#f0f0f0' } }
          ]
        }],
        graphic: {
          elements: [{
            type: 'text',
            left: 'center',
            top: 'middle',
            style: {
              text: `${usage}%`,
              fontSize: 16,
              fontWeight: 'bold'
            }
          }]
        }
      }, { notMerge: false })
    }

    // --- 折线面积图 ---
    const lineEl = diskLineRefs.value[drive]
    if (lineEl && !diskLineInstances[drive]) {
      diskLineInstances[drive] = echarts.init(lineEl)
      diskLineInstances[drive].setOption({
        tooltip: { trigger: 'axis' },
        xAxis: { type: 'category', data: formatTimeLabels() },
        yAxis: { type: 'value', min: 0, max: 100 },
        series: [{
          type: 'line',
          areaStyle: {},
          data: history.disks[drive],
          smooth: true,
          color: color,
          showSymbol: false,
          animationDuration: 800,
          animationEasing: 'easeOutQuint'
        }]
      }, true)
    }

    if (diskLineInstances[drive]) {
      diskLineInstances[drive].setOption({
        xAxis: { data: formatTimeLabels() },
        series: [{
          type: 'line',
          data: history.disks[drive],
          color: color
        }]
      }, { notMerge: false })
    }
  })
}

// 格式化时间标签
const formatTimeLabels = () => {
  return history.timestamps.map(t =>
    new Date(t).toLocaleTimeString([], {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit'
    })
  )
}

// WebSocket 消息处理
const handler = (msg) => {
  const msgJson = safeJsonParse(msg)
  if (msgJson?.Operation === 'MachineStatus') {
    updateHistory(msgJson)
  }
}

let intervalId = null

onMounted(() => {
  websocketStore.registerMessageHandler(handler)

  const reqJson = { Operation: 'MachineStatus' }
  websocketStore.sendMessage(JSON.stringify(reqJson))

  intervalId = setInterval(() => {
    websocketStore.sendMessage(JSON.stringify(reqJson))
  }, 2500)
})

onUnmounted(() => {
  websocketStore.unregisterMessageHandler(handler)
  if (intervalId) clearInterval(intervalId)

  // 清理 ECharts 实例
  cpuChartInstance?.dispose()
  memoryChartInstance?.dispose()
  Object.values(diskPieInstances).forEach(c => c?.dispose())
  Object.values(diskLineInstances).forEach(c => c?.dispose())
})
</script>

<style scoped>
.machine-monitor {
  padding: 16px;
  background-color: #f9fafb;
}

/* 通用图表行容器 */
.chart-row {
  display: flex;
  gap: 20px;
  margin-bottom: 24px;
  width: 100%;
}

/* 移动端垂直布局 */
.mobile-column {
  flex-direction: column;
  gap: 16px;
}

/* 图表项：CPU / 内存 / 磁盘面积图 */
.chart-item,
.disk-line {
  flex: 1;
  min-height: 240px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
  overflow: hidden;
}

/* 桌面端磁盘区域标题 */
.disk-row h3 {
  margin: 0 0 12px 0;
  color: #333;
  font-size: 1.1rem;
}

/* 磁盘饼图：桌面端固定尺寸，移动端居中 */
.disk-pie {
  width: 180px;
  height: 180px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.1);
}

/* 移动端适配 */
@media (max-width: 959px) {
  .machine-monitor {
    padding: 12px;
  }

  .chart-item,
  .disk-line {
    min-height: 260px; /* 稍微加高，避免数据拥挤 */
    width: 100%;
  }

  .disk-pie {
    width: 100%;
    max-width: 200px;
    height: auto;
    aspect-ratio: 1 / 1; /* 保持正方形 */
    align-self: center;
  }

  .chart-row {
    gap: 16px;
    margin-bottom: 20px;
  }
}
</style>
