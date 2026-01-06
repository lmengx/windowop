// src/stores/websocket.js
import { ref, reactive } from 'vue'

import { useRouter } from 'vue-router'

import { defineStore } from 'pinia'

import { useDataStore } from './dataStore.js';

import { safeJsonParse } from '@/composables/useSafeJsonParse.js'

  import CryptoJS from 'crypto-js';
  import JSEncrypt from 'jsencrypt';

export const useWebSocketStore = defineStore('websocket', () => {

  const router = useRouter()

  const DataStore = useDataStore()




  const Target = ref({})
  const AES_key = ref('')//对称加密凭据
  const HmacKey = ref('')
  const Verified = ref(false)

  // 响应式状态
  const socket = ref(null)
  const isConnected = ref(false)
  const error = ref(null)
  const reconnectAttempts = ref(0)
  const maxReconnectAttempts = 5

  // 存储所有子组件注册的消息处理器
  const messageHandlers = reactive([])

  // 心跳定时器
  let heartbeatTimer = null

  // ========================
  // 核心方法
  // ========================

  function connect(TargetData)
  {
    Target.value = TargetData
    resetConnectionState()
    let url = Target.value.address
    if(url == "[visitTarget]") url = window.location.origin

    socket.value = new WebSocket(url)

    socket.value.onopen = () => {
      isConnected.value = true
      reconnectAttempts.value = 0
      startHeartbeat()
      DataStore.Log('✅ WebSocket connected')
    }

    socket.value.onmessage = async (event) => {
      const msg = await RecievedRawMsg(event.data)
      messageHandlers.forEach(handler => handler(msg))
    }

    socket.value.onclose = () => {
      isConnected.value = false
      stopHeartbeat()
      console.log('🔌 WebSocket closed')




      if(Verified.value)
      {
        Verified.value = false;
          ElMessageBox.confirm(
            '连接已断开，是否返回主页',
            'Warning',
            {
              confirmButtonText: '确定',
              cancelButtonText: '取消',
              type: 'warning',
              center: true,
              closeOnClickModal: false,
              showClose: false,
            }
          )
            .then(() => {
              router.push("/connect")
            }).catch(() =>{

            })
      }


    }

    socket.value.onerror = (err) => {
      error.value = err.message || 'Unknown error'
      console.error('❌ WebSocket error:', err)
          ElNotification({
          title: '连接失败',
          type: 'error',
        })


    }
  }


function sendMessage(msg) {
  let finalMsg = msg;

  // 尝试判断 msg 是否为 JSON 字符串
  if (typeof msg === 'string')
  {
    try {
      const parsed = JSON.parse(msg);
      // 确保解析结果是对象（排除 '123'、'"text"' 等非对象 JSON）
      if (parsed && typeof parsed === 'object' && !Array.isArray(parsed)) {
        // 添加 timestamp（毫秒时间戳）
        parsed.Timestamp = Date.now();
        // 重新序列化为字符串
        finalMsg = JSON.stringify(parsed);
      }
    } catch (e) {
      // 不是合法 JSON，保持原样
    }
  }

  const sendData = AES_en(finalMsg);
  sendRawMessage(sendData);
  DataStore.Log(`↑发送↑: ${finalMsg}`);
}

  function sendRawMessage(data) {
    if (!socket.value || socket.value.readyState !== WebSocket.OPEN) {
      console.warn('⚠️ Cannot send: WebSocket not open')
      return false
    }

    const payload = typeof data === 'string' ? data : JSON.stringify(data)
    socket.value.send(payload)
    DataStore.Log("发送原始消息:"+data)
    return true
  }

  // ========================
  // 消息处理器管理
  // ========================

  function registerMessageHandler(handler) {
    messageHandlers.push(handler)
  }

  function unregisterMessageHandler(handler) {
    const index = messageHandlers.indexOf(handler)
    if (index > -1) {
      messageHandlers.splice(index, 1)
    }
  }

  // ========================
  // 连接控制
  // ========================

  function startHeartbeat() {
    stopHeartbeat()
    heartbeatTimer = setInterval(() => {
      if (isConnected.value && Verified.value) {
        sendMessage(JSON.stringify({ Operation: 'heartbeat' }))
      }
    }, 30000) // 30秒一次心跳
  }

  function stopHeartbeat() {
    if (heartbeatTimer) {
      clearInterval(heartbeatTimer)
      heartbeatTimer = null
    }
  }

  function attemptReconnect(url) {
    if (reconnectAttempts.value < maxReconnectAttempts) {
      reconnectAttempts.value++
      setTimeout(() => {
        if (!isConnected.value) {
          connect(url)
        }
      }, 2000 * reconnectAttempts.value) // 指数退避重连
    }
  }

  function resetConnectionState() {
    isConnected.value = false
    error.value = null
    reconnectAttempts.value = 0
    stopHeartbeat()
  }

  function close() {
    if (socket.value) {
      socket.value.close()
      resetConnectionState()
    }
  }

// ==========
// 消息收发函数
// ==========

  var RSA_pk

  async function RecievedRawMsg(eventData)
  {
    DataStore.Log("收到原始消息:"+eventData)
    const msgJson = safeJsonParse(eventData)

    if (msgJson.Operation == "CryptedMsg")
    {
      const decryptedText = AES_de(msgJson.CryptedMsg)
      DataStore.Log("解密得到消息：" + decryptedText)
      return decryptedText
    }


    if (msgJson.Operation == "IniKey")
    {
      RSA_pk = msgJson.RSA_pk
      HmacKey.value = msgJson.HmacKey

      if (HmacKey.value == "PwdNotSet") return eventData

        iv = GetKey(16)
        const sendData = RSA_en(iv, RSA_pk)
        sendRawMessage(sendData)

        if (HmacKey.value == Target.value.HmacKey)//已保存的登录凭据和服务端吻合，自动尝试验证
        {
          AES_key.value = Target.value.HashedPwd
          const msg = `{"Operation": "Verify"}`
          sendMessage(msg)
        }
    }
    return eventData


  }



  function SetPwd(pwd) {
    HmacKey.value = GetKey(16)
    const HashedPwd = HmacSha256(pwd, HmacKey.value).substring(0, 16);
    const PwdData = `{"HashedPwd": "${HashedPwd}","HmacKey": "${HmacKey.value}"}`;
    const sendData = RSA_en(PwdData, RSA_pk)
    sendRawMessage(sendData)
  }

  const RemenberPwd = ref(false)
    function VerifyPwd(pwd, Remenber)
    {
    RemenberPwd.value = Remenber
    AES_key.value = HmacSha256(pwd, HmacKey.value).substring(0, 16);
    const msg = `{"Operation": "Verify"}`
    sendMessage(msg)
  }

    function ChangePwd(oldPwd, newPwd) {
    HmacKey.value = GetKey(16)
    newPwd = HmacSha256(newPwd, HmacKey.value).substring(0, 16);
    const reqJson = JSON.stringify({
      Operation: 'ChangePassword',
      OldPassword: oldPwd,
      NewPassword: newPwd,
      HmacKey: HmacKey.value
    });
    sendMessage(reqJson)
  }



  // =========
  // 加密函数
  // =========

  function HmacSha256(data, key) {
    return CryptoJS.HmacSHA256(data, key).toString(CryptoJS.enc.Hex);
  }

  function RSA_en(data, publicKey) {
    const encrypt = new JSEncrypt();
    encrypt.setPublicKey(publicKey);
    return encrypt.encrypt(data);
  }

  const mode = "CBC";               //mode ECB CBC CFB OFB CTR
  const pad = "Pkcs7";              //padding Pkcs7 Iso10126 NoPadding ZeroPadding
  const keyType = "Utf8";           //Utf8 Base64 Hex
  var iv = "";         //iv AES-16byte DES-8byte 3DES-8byte
  const ivType = "Utf8";            //Utf8 Base64 Hex
  const isBase64 = false;           //待解密数据编码 true: Base64, false: Hex

  function AES_en(data) {
    const crypto_key = CryptoJS.enc[keyType].parse(AES_key.value);
    let cfg = {};
    cfg.iv = CryptoJS.enc[ivType].parse(iv);
    cfg.mode = CryptoJS.mode[mode];
    cfg.padding = CryptoJS.pad[pad];

    let result = CryptoJS.AES.encrypt(data, crypto_key, cfg).ciphertext.toString(isBase64 ? CryptoJS.enc.Base64 : CryptoJS.enc.Hex)

    return result
  };

  function AES_de(data) {
    const crypto_key = CryptoJS.enc[keyType].parse(AES_key.value);
    let cfg = {};
    cfg.iv = CryptoJS.enc[ivType].parse(iv);
    cfg.mode = CryptoJS.mode[mode];
    cfg.padding = CryptoJS.pad[pad];

    const cryptoData = isBase64 ? data : CryptoJS.enc.Base64.stringify(CryptoJS.enc.Hex.parse(data));
    const decrypt = CryptoJS.AES.decrypt(cryptoData, crypto_key, cfg)
    const result = CryptoJS.enc.Utf8.stringify(decrypt);
    return result
  }

  function GetKey(length) {
    let result = '';
    for (let i = 0; i < length; i++) {result += Math.floor(Math.random() * 10).toString();}
    result = result.split('').sort(() => Math.random() - 0.5).join('');
    const base64String = btoa(result);
    return base64String.substring(0, length);
  }




    // 暴露给组件使用的接口
  return {
    // 状态
    Target,
    isConnected,
    error,
    Verified,
    HmacKey,
    AES_key,
    RemenberPwd,
    // 方法
    connect,
    sendMessage,
    SetPwd,
    VerifyPwd,
    ChangePwd,
    registerMessageHandler,
    unregisterMessageHandler,
    close,
  }

})
