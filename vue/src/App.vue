
<template>

  <div class="fullscreen" v-auto-animate>
    <Loading v-if="showPage == 'Loading'"
             :Disconnected="Disconnected" />

    <Ini v-if="showPage == 'Ini'"
         @SetPwd="SetPwd" />

    <VerifyModule class="fullscreen centerbox"
                  v-if="showPage == 'VerifyModal'"
                  @VerifyPwd="VerifyPwd" />

    <MainContent class="fullscreen"
                 v-if="showPage == 'MainContent'" />

  </div>

</template>


<script setup>
  import { ref, onMounted, onUnmounted, provide, watch } from 'vue';
  import { usePwdStore } from './stores/Pwd.js'
  import ShowModal from './components/ShowModal.vue';
  import Loading from "./components/Loading.vue";
  import Ini from "./components/Ini.vue"
  import VerifyModule from './components/VerifyModule.vue';
  import MainContent from './components/MainContent.vue';
  import CryptoJS from 'crypto-js';
  import JSEncrypt from 'jsencrypt';

  

  onMounted(() => {
    if (currentPath === '/debug')
      ElNotification({
        title: "已进入调试页面",
        message: "可以按F12进入控制台查看输出",
        type: "info",
      })
    if (currentPath === '/develop')
      ElNotification({
        title: "进入开发页面",
        message: "websocket连接将指向本机的7799端口",
        type: "info",
      })

    connect();
  });

  const currentPath = window.location.pathname;
  function Log(msg) {
    if (currentPath === '/debug' || currentPath === "/develop") console.log(msg)
  }

  // 控制渲染的变量

  const showPage = ref('Loading')
  const alertModal = ref(null);
  const Disconnected = ref(false);

  var ws = null;

  const receivedMsg = ref({
    text: '',
    updateCount: 0 // 用于触发 watch 的计数器
  });

  const PwdStore = usePwdStore()
  var RemenberPwd = false
  const AES_key = ref("")


  provide("provideFuncSendWSMsg",  SendMsg);
  provide("provideReceivedMsg", receivedMsg);
  provide("provideFuncLog", Log);
  provide("provideFuncChangePwd", ChangePwd);
  provide("provideFuncChangePage_app", ChangePage_app);
  provide("provideAES_key", AES_key);


  function ChangePage_app(pageName) {
    showPage.value = pageName
  }

    function SendRawMsg(msg)
    {
      if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(msg);
        Log(`↑发送↑: ${msg}`);
        }
        else Log("发送失败，ws未连接")
  }

  function SendMsg(msg) {
    const sendData = AES_en(msg)
    SendRawMsg(sendData)
    Log(`↑发送↑: ${msg}`);
  }


  watch(receivedMsg, (newVal) => {
     RecievedMsg(newVal.text)
  });

  function safeJsonParse(str) {
    try {
      return JSON.parse(str);
    } catch (e) {
      return null;
    }
  }
    function RecievedMsg(eventData)
    {
      const eventJson = safeJsonParse(eventData)
      if (eventJson != null) {
        if (eventJson.Operation == "Verified") {
          if (RemenberPwd) {
            PwdStore.HashedPwd = AES_key.value;
            PwdStore.HmacKey = HmacKey;
          }
          showPage.value = "MainContent"
        }

        else if (eventJson.Operation == "Notification") {
          ElNotification({
            title: eventJson.title,
            message: eventJson.message,
            type: eventJson.type,
          })
        }

        else if (eventJson.Operation == "Download") {
          const ws1 = new DownloadWS('');//判断类型，文件夹则为zip

        }

        else if (eventJson.Operation == "Error") {
          console.error(eventJson.message);
        }

      }


    }

  var HmacKey;
  var RSA_pk;

  function RecievedRawMsg(eventData) {
    const msgJson = JSON.parse(eventData)
    if (msgJson.Operation == "IniKey") {
      RSA_pk = msgJson.RSA_pk
      HmacKey = msgJson.HmacKey
      if (HmacKey == "PwdNotSet") {
        showPage.value = "Ini"
      }
      else {
        iv = GetKey(16)
        const sendData = RSA_en(iv, RSA_pk)
        SendRawMsg(sendData)
        if (HmacKey == PwdStore.HmacKey) {
          AES_key.value = PwdStore.HashedPwd
          const msg = `{"Operation": "Verify"}`
          SendMsg(msg)
        }
      }
    }
    else if (msgJson.Operation == "ivReceived") {
      showPage.value = "VerifyModal"
    }
    else if (msgJson.Operation == "CryptedMsg") {
      const decryptedText = AES_de(msgJson.CryptedMsg)
      receivedMsg.value = {
        text: decryptedText,
        updateCount: receivedMsg.value.updateCount + 1 // 增加计数器
      };
      Log("解密得到消息：" + decryptedText)
    }
    else if (msgJson.Operation == "AESCouldNotDecrypt") {
      Log("发出的消息无法解密")
      if (showPage.value == "VerifyModal") {
        ElNotification({
          title: '密码错误',
          type: 'error',
        })
          PwdStore.HashedPwd = "";
          PwdStore.HmacKey = "";
      }
    }

  }

  function SetPwd(pwd) {
    const HmacKey = GetKey(16)
    const HashedPwd = HmacSha256(pwd, HmacKey).substring(0, 16);
    const PwdData = `{"HashedPwd": "${HashedPwd}","HmacKey": "${HmacKey}"}`;
    const sendData = RSA_en(PwdData, RSA_pk)
    SendRawMsg(sendData)
  }

  function VerifyPwd(pwd, Remenber) {
    RemenberPwd = Remenber
    AES_key.value = HmacSha256(pwd, HmacKey).substring(0, 16);
    const msg = `{"Operation": "Verify"}`
    SendMsg(msg)
  }

  function ChangePwd(oldPwd, newPwd) {
    const HmacKey = GetKey(16)
    newPwd = HmacSha256(newPwd, HmacKey).substring(0, 16);
    const reqJson = JSON.stringify({
      Operation: 'ChangePassword',
      OldPassword: oldPwd,
      NewPassword: newPwd,
      HmacKey: HmacKey
    });
    SendMsg(reqJson)
  }

    function connect() {

        const httpUrl = window.location.href;
        const url = new URL(httpUrl);
        var wsUrl = `${url.protocol === 'https:' ? 'wss:' : 'ws:'}//${url.hostname}:${url.port || 7799}/`;
      if (currentPath === "/develop") wsUrl = "ws://127.0.0.1:7799"
        ws = new WebSocket(wsUrl);
        ws.onopen = () => {
            Log('WebSocket已连接');
        };

        ws.onmessage = (event) => {
            RecievedRawMsg(event.data)
        };

        ws.onclose = () => {
            Log('WebSocket已断开');
            showPage.value = "Loading"
            Disconnected.value = true; // 设置为连接断开状态
        };

        ws.onerror = (error) => {
            Log('WebSocket 连接错误:', error);
            showPage.value = "Loading"
            Disconnected.value = true; // 设置为连接断开状态
        };
    }

    onUnmounted(() => {
        if (ws) {
            ws.close(); // 组件卸载时关闭 WebSocket 连接
        }
    });


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

</script>
