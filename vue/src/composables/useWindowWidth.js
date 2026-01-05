// composables/useWindowWidth.js
import { ref, onMounted, onUnmounted } from 'vue'

export function useWindowWidth() {
  const windowWidth = ref(0)

  const updateWidth = () => {
    // 确保在浏览器环境中执行（避免 SSR 报错）
    if (typeof window !== 'undefined') {
      windowWidth.value = window.innerWidth
    }
  }

  onMounted(() => {
    updateWidth() // 初始化
    window.addEventListener('resize', updateWidth)
  })

  onUnmounted(() => {
    window.removeEventListener('resize', updateWidth)
  })

  return { windowWidth }
}
