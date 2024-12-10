
<template>
    <ShowModal ref="alertModal" />
    <Loading v-if="showPage == 'Loading'"
             :Disconnected="Disconnected" />

    <Ini v-if="showPage == 'Ini'"
         class="fullscreen centerbox"
         @SetPwd="SetPwd"
         />

    <VerifyModule class="fullscreen centerbox"
                  v-if="showPage == 'VerifyModal'"
                  @VerifyPwd="VerifyPwd"
                   />

    <MainContent  class="fullscreen"
                 v-if="showPage == 'MainContent'" />



</template>


<script setup>
    import { ref, onMounted, onUnmounted, provide, watch } from 'vue';
    import ShowModal from './components/ShowModal.vue';
    import Loading from "./components/Loading.vue";
    import Ini from "./components/Ini.vue"
    import VerifyModule from './components/VerifyModule.vue';
    import MainContent from './components/MainContent.vue';
  import CryptoJS from 'crypto-js';
  import JSEncrypt from 'jsencrypt';


    var ws = null;

  const receivedMsg = ref({
    text: '',
    updateCount: 0 // 用于触发 watch 的计数器
  });

  provide("provideFuncSendWSMsg",  SendMsg);
  provide("provideReceivedMsg", receivedMsg);
  provide("provideFuncLog", Log);
  provide("provideFuncChangePwd", ChangePwd);


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
  }


  const currentPath = window.location.pathname;
  function Log(msg) {
    if (currentPath === '/debug') console.log(msg)
  }

    // 控制渲染的变量

    const showPage = ref('Loading')
    const alertModal = ref(null);
    const Disconnected = ref(false);


  watch(receivedMsg, (newVal) => {
     RecievedMsg(newVal.text)
  });

    function RecievedMsg(eventData)
    {
      if (eventData == "Verified") showPage.value = "MainContent"
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
      }
    }
    else if (msgJson.Operation == "ivReceived") showPage.value = "VerifyModal"
    else if (msgJson.Operation == "CryptedMsg") {
      const decryptedText = AES_de(msgJson.CryptedMsg)
      receivedMsg.value = {
        text: decryptedText,
        updateCount: receivedMsg.value.updateCount + 1 // 增加计数器
      };
      Log("解密得到消息：" + decryptedText)
    }
   
    
  }

  function SetPwd(pwd) {
    const HmacKey = GetKey(16)
    const HashedPwd = HmacSha256(pwd, HmacKey).substring(0, 16);
    const PwdData = `{"HashedPwd": "${HashedPwd}","HmacKey": "${HmacKey}"}`;
    const sendData = RSA_en(PwdData, RSA_pk)
    SendRawMsg(sendData)   
  }

  function VerifyPwd(pwd) {
    AES_key = HmacSha256(pwd, HmacKey).substring(0, 16);
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
        //wsUrl = "ws://127.0.0.1:7799"
        ws = new WebSocket(wsUrl);
        ws.onopen = () => {
            Log('WebSocket已连接');
        };

        ws.onmessage = (event) => {
            Log(`↓收到↓: ${event.data}`);
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

  onMounted(() => {
    if (currentPath === '/debug') console.log('已进入调试页面，可以按F12进入控制台查看输出\n--这条信息需要改为提示--\n')
      connect(); // 组件挂载时连接 WebSocket
    });

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

  var AES_key = ""
  const mode = "CBC";               //mode ECB CBC CFB OFB CTR
  const pad = "Pkcs7";              //padding Pkcs7 Iso10126 NoPadding ZeroPadding
  const keyType = "Utf8";           //Utf8 Base64 Hex
  var iv = "";         //iv AES-16byte DES-8byte 3DES-8byte
  const ivType = "Utf8";            //Utf8 Base64 Hex
  const isBase64 = false;           //待解密数据编码 true: Base64, false: Hex

  function AES_en(data) {
    const crypto_key = CryptoJS.enc[keyType].parse(AES_key);
    let cfg = {};
    cfg.iv = CryptoJS.enc[ivType].parse(iv);
    cfg.mode = CryptoJS.mode[mode];
    cfg.padding = CryptoJS.pad[pad];

    let result = CryptoJS.AES.encrypt(data, crypto_key, cfg).ciphertext.toString(isBase64 ? CryptoJS.enc.Base64 : CryptoJS.enc.Hex)

    return result
  };

  function AES_de(data) {
    const crypto_key = CryptoJS.enc[keyType].parse(AES_key);
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
