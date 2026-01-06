<template>
  <div class="remote-container">
    <canvas ref="screenCanvas"
            @mousemove="handleMouseMove"
            @mousedown="handleMouseDown"
            @mouseup="handleMouseUp"
            @wheel="handleMouseWheel"
            @keydown.prevent="handleKeyDown"
            @keyup.prevent="handleKeyUp"
            tabindex="0"></canvas>

    <div v-if="connectionStatus !== 'connected'" class="status-overlay">
      {{ statusMessages[connectionStatus] }}
    </div>
  </div>
</template>

<script setup>
  import { ref, inject, onMounted, onBeforeUnmount, watchEffect } from 'vue'
  import CryptoJS from 'crypto-js'

  import { useDataStore } from '@/stores/dataStore'
  import { useWebSocketStore } from '@/stores/websocketStore'

  const dataStore = useDataStore()
  const websocketStore = useWebSocketStore()

  const props = defineProps({
    currentPath: String
  })

  const Log = dataStore.Log;

  // 依赖注入
  const AESKey = websocketStore.AES_key
  const screenCanvas = ref(null)
  let ctx = null
  let ws = null
  let aesIV = null // 作为 Uint8Array 存储，直接从服务器接收

  // 状态管理
  const connectionStatus = ref('connecting')
  const statusMessages = {
    connecting: '连接中...',
    connected: '',
    disconnected: '连接断开',
    error: '连接错误'
  }

  // 尺寸管理
  const canvasSize = ref({ width: 0, height: 0 }) // 远程屏幕的原始尺寸 (例如: 1920x1080)
  const displayRatio = ref(1) // 远程屏幕原始尺寸 / canvas显示尺寸 的比率

  // --- WebSocket 初始化 ---
  const initWebSocket = () => {
    const getWsUrl = () => {

      return websocketStore.Target.address+"/remotedesk";

    }

    ws = new WebSocket(getWsUrl())
    ws.binaryType = 'arraybuffer' // 确保接收二进制数据

    ws.addEventListener('open', handleWsOpen)
    ws.addEventListener('message', handleWsMessage)
    ws.addEventListener('error', handleWsError)
    ws.addEventListener('close', handleWsClose)
  }

  // --- WebSocket 事件处理函数 ---
  const handleWsOpen = () => {
    Log('WebSocket 连接已建立')
    connectionStatus.value = 'connected'
    // canvas 获取焦点，以便接收键盘事件
    if (screenCanvas.value) {
      screenCanvas.value.focus()
    }
  }

  const handleWsMessage = async (event) => {
    try {
      const data = event.data

      // 如果 IV 未设置，则第一个接收到的消息是 IV
      if (!aesIV) {
        aesIV = new Uint8Array(data) // 直接存储为 Uint8Array
        Log(`收到 IV 数据，长度: ${aesIV.length}`)
        return
      }

      // 屏幕数据解密和渲染
      const decryptedWordArray = decryptData(data)
      if (decryptedWordArray) {
        // 将 CryptoJS WordArray 转换为 ArrayBuffer
        const imageData = wordArrayToArrayBuffer(decryptedWordArray)
        renderFrame(imageData)
      }
    } catch (error) {
      console.error(`消息处理错误: ${error.message}`, error)
      connectionStatus.value = 'error'
    }
  }

  const handleWsError = (error) => {
    console.error(`WebSocket 错误:`, error)
    connectionStatus.value = 'error'
  }

  const handleWsClose = (event) => {
    Log(`连接关闭，代码: ${event.code}，原因: ${event.reason}`)
    connectionStatus.value = 'disconnected'
    ws = null // 清除 WebSocket 实例
  }

  // --- AES 加密/解密配置和函数 ---
  const getCryptoConfig = () => ({
    mode: CryptoJS.mode.CBC,
    padding: CryptoJS.pad.Pkcs7,
    iv: aesIV ? CryptoJS.lib.WordArray.create(aesIV) : undefined
  })

  const decryptData = (encryptedBuffer) => {
    if (!aesIV) {
      console.warn('IV 尚未接收，无法解密')
      return null
    }
    if (!AESKey) {
      console.error('AESKey 未提供或无效，无法解密。')
      return null;
    }

    const encryptedWordArray = CryptoJS.lib.WordArray.create(encryptedBuffer)

    return CryptoJS.AES.decrypt(
      { ciphertext: encryptedWordArray },
      CryptoJS.enc.Utf8.parse(AESKey),
      getCryptoConfig()
    )
  }

  const encryptData = (plainBuffer) => {
    if (!aesIV) {
      console.warn('IV 尚未接收，无法加密')
      return null
    }
    if (!AESKey) {
      console.error('AESKey 未提供或无效，无法加密。')
      return null;
    }

    const plainWordArray = CryptoJS.lib.WordArray.create(plainBuffer)

    const encrypted = CryptoJS.AES.encrypt(
      plainWordArray,
      CryptoJS.enc.Utf8.parse(AESKey),
      getCryptoConfig()
    )

    return encrypted.ciphertext
  }

  // --- 命令发送 ---
  const sendCommand = (commandType, data) => {
    if (!ws || ws.readyState !== WebSocket.OPEN || !aesIV) {
      console.warn('WebSocket 未连接或 IV 未接收，无法发送命令')
      return
    }

    const header = new ArrayBuffer(4)
    new DataView(header).setUint32(0, commandType, true) // 小端序

    const payload = concatArrayBuffers(header, data) // 将命令头和数据合并

    const encryptedWordArray = encryptData(payload)
    if (!encryptedWordArray) return

    const encryptedBytes = wordArrayToArrayBuffer(encryptedWordArray)

    ws.send(encryptedBytes)
  }

  // --- 输入事件处理 ---
  const handleMouseMove = (e) => {
    if (e.buttons === 1 || e.buttons === 0) {
      const { clientX, clientY } = e
      const { offsetX, offsetY } = calculateCanvasPosition(clientX, clientY)

      const data = new ArrayBuffer(8)
      const view = new DataView(data)
      view.setInt32(0, offsetX, true)
      view.setInt32(4, offsetY, true)
      sendCommand(0x01, data)
    }
  }

  const handleMouseDown = (e) => {
    e.preventDefault()
    const { clientX, clientY } = e
    const { offsetX, offsetY } = calculateCanvasPosition(clientX, clientY)

    let buttonState;
    if (e.button === 0) {
      buttonState = 0x01;
    } else if (e.button === 2) {
      buttonState = 0x02;
    } else {
      return;
    }

    const data = new ArrayBuffer(9);
    const view = new DataView(data);
    view.setInt32(0, offsetX, true);
    view.setInt32(4, offsetY, true);
    view.setUint8(8, buttonState);
    sendCommand(0x02, data);

    if (screenCanvas.value) {
      screenCanvas.value.focus();
    }
  }

  const handleMouseUp = (e) => {
    e.preventDefault();
    const { clientX, clientY } = e;
    const { offsetX, offsetY } = calculateCanvasPosition(clientX, clientY);

    let buttonState;
    if (e.button === 0) {
      buttonState = 0x04;
    } else if (e.button === 2) {
      buttonState = 0x08;
    } else {
      return;
    }

    const data = new ArrayBuffer(9);
    const view = new DataView(data);
    view.setInt32(0, offsetX, true);
    view.setInt32(4, offsetY, true);
    view.setUint8(8, buttonState);
    sendCommand(0x02, data);
  }

  const handleMouseWheel = (e) => {
    e.preventDefault();
    const { clientX, clientY } = e;
    const { offsetX, offsetY } = calculateCanvasPosition(clientX, clientY);

    const data = new ArrayBuffer(12);
    const view = new DataView(data);
    view.setInt32(0, offsetX, true);
    view.setInt32(4, offsetY, true);
    view.setInt32(8, e.deltaY, true);
    sendCommand(0x05, data);
  }

  const handleKeyDown = (e) => {
    handleKeyEvent(e, true)
  }

  const handleKeyUp = (e) => {
    handleKeyEvent(e, false)
  }

  const handleKeyEvent = (e, isDown) => {
    const data = new ArrayBuffer(3)
    const view = new DataView(data)
    view.setUint16(0, e.keyCode, true)
    view.setUint8(2, isDown ? 1 : 0)
    sendCommand(0x03, data)
  }

  // --- 图像渲染 ---
  const renderFrame = (imageData) => {
    const blob = new Blob([imageData], { type: 'image/jpeg' })
    const url = URL.createObjectURL(blob)

    const img = new Image()
    img.onload = () => {
      // 只有当屏幕分辨率改变时才更新 canvas 的实际尺寸
      if (canvasSize.value.width !== img.width || canvasSize.value.height !== img.height) {
        canvasSize.value = { width: img.width, height: img.height }
        // 设置 canvas 的绘制尺寸（内部像素）与远程屏幕原始尺寸一致
        screenCanvas.value.width = img.width
        screenCanvas.value.height = img.height
        Log(`画布内部分辨率设置为: ${img.width}x${img.height}`);
        updateCanvasDimensions() // 根据新的原始尺寸更新显示尺寸
      }

      ctx.drawImage(img, 0, 0, canvasSize.value.width, canvasSize.value.height)
      URL.revokeObjectURL(url)
    }
    img.onerror = (err) => {
      console.error("图片加载失败", err);
      URL.revokeObjectURL(url);
    };
    img.src = url
  }

  // --- 辅助函数 ---
  const arrayBufferToBase64 = (buffer) => {
    let binary = '';
    const bytes = new Uint8Array(buffer);
    const len = bytes.byteLength;
    for (let i = 0; i < len; i++) {
      binary += String.fromCharCode(bytes[i]);
    }
    return btoa(binary);
  }

  const wordArrayToArrayBuffer = (wordArray) => {
    const len = wordArray.sigBytes;
    const bytes = new Uint8Array(len);
    for (let i = 0; i < len; i++) {
      bytes[i] = (wordArray.words[i >>> 2] >>> (24 - (i % 4) * 8)) & 0xff;
    }
    return bytes.buffer;
  }

  const concatArrayBuffers = (...arrays) => {
    const totalLength = arrays.reduce((acc, arr) => acc + arr.byteLength, 0)
    const result = new Uint8Array(totalLength)
    let offset = 0
    arrays.forEach(arr => {
      result.set(new Uint8Array(arr), offset)
      offset += arr.byteLength
    })
    return result.buffer
  }

  // 计算鼠标在远程屏幕上的实际坐标
  const calculateCanvasPosition = (clientX, clientY) => {
    if (!screenCanvas.value || canvasSize.value.width === 0) {
      return { offsetX: 0, offsetY: 0 };
    }
    const rect = screenCanvas.value.getBoundingClientRect();

    const mouseX = clientX - rect.left;
    const mouseY = clientY - rect.top;

    const scaledX = Math.round(mouseX * displayRatio.value);
    const scaledY = Math.round(mouseY * displayRatio.value);

    const finalX = Math.max(0, Math.min(scaledX, canvasSize.value.width - 1));
    const finalY = Math.max(0, Math.min(scaledY, canvasSize.value.height - 1));

    Log(`鼠标坐标命令: (${clientX}, ${clientY}), 整流: (${rect.left}, ${rect.top}), 鼠标活动区: (${mouseX}, ${mouseY}), Scaled to remote: (${finalX}, ${finalY}), 显示率: ${displayRatio.value}`);

    return { offsetX: finalX, offsetY: finalY };
  }

  // 更新 canvas 显示尺寸以适应容器并保持比例
  const updateCanvasDimensions = () => {
    if (!screenCanvas.value || !screenCanvas.value.parentElement || canvasSize.value.width === 0 || canvasSize.value.height === 0) {
      console.warn('无法更新画布尺寸：缺少画布参考或父级，或者canvasSize为零.\n--传输结束时有一次此提示为正常现象');
      return;
    }

    const container = screenCanvas.value.parentElement;
    let containerWidth = container.clientWidth;
    let containerHeight = container.clientHeight;
    const aspectRatio = canvasSize.value.width / canvasSize.value.height;
    const containerAspect = containerWidth / containerHeight;

    let finalDisplayWidth;
    let finalDisplayHeight;

    if (aspectRatio > containerAspect) {
      finalDisplayWidth = containerWidth;
      finalDisplayHeight = containerWidth / aspectRatio;
    } else {
      finalDisplayHeight = containerHeight;
      finalDisplayWidth = containerHeight * aspectRatio;
    }

    // 设置 Canvas 元素的 CSS 尺寸
    screenCanvas.value.style.width = `${finalDisplayWidth}px`;
    screenCanvas.value.style.height = `${finalDisplayHeight}px`;

    displayRatio.value = canvasSize.value.width / finalDisplayWidth;

    Log(`画布面积: ${containerWidth}x${containerHeight}, 图像面积: ${canvasSize.value.width}x${canvasSize.value.height}, Canvas 显示: ${finalDisplayWidth}x${finalDisplayHeight}, 显示率: ${displayRatio.value}`);
  }

  // --- 生命周期钩子 ---
  onMounted(() => {
    ctx = screenCanvas.value.getContext('2d')
    initWebSocket()
    window.addEventListener('resize', updateCanvasDimensions)
    if (screenCanvas.value) {
      screenCanvas.value.addEventListener('contextmenu', (e) => e.preventDefault());
    }
    updateCanvasDimensions(); // 初始时调用一次尺寸更新
  })

  onBeforeUnmount(() => {
    ws?.close()
    window.removeEventListener('resize', updateCanvasDimensions)
    if (screenCanvas.value) {
      screenCanvas.value.removeEventListener('contextmenu', (e) => e.preventDefault());
    }
  })

  // 监听 canvasSize 的变化以更新显示
  watchEffect(() => {
    if (canvasSize.value.width > 0 && canvasSize.value.height > 0) {
      updateCanvasDimensions();
    }
  });
</script>

<style scoped>
  .remote-container {
    /* 确保这个容器能填满父组件分配给它的所有空间 */
    width: 100%; /* 相对于父组件的宽度 */
    height: 100%; /* 相对于父组件的高度 */
    position: relative; /* 保持 relative 以便 status-overlay 定位 */
    overflow: hidden; /* 隐藏超出容器的内容 */
    background: #1a1a1a;
    display: flex; /* 使用 flex 布局来居中 canvas */
    justify-content: center; /* 水平居中 */
    align-items: center; /* 垂直居中 */
  }

  canvas {
    max-width: 100%;
    max-height: 100%;
    display: block;
    object-fit: contain;
    image-rendering: -webkit-optimize-contrast;
    image-rendering: crisp-edges;
    cursor: none;
    outline: none;
    transition: width 0.1s ease-out, height 0.1s ease-out;
  }

  .status-overlay {
    position: absolute;
    top: 20px;
    left: 50%;
    transform: translateX(-50%);
    padding: 10px 20px;
    background: rgba(0,0,0,0.7);
    color: white;
    border-radius: 5px;
    font-family: monospace;
    z-index: 10;
  }
</style>
