<template>
  <div class="ai-chat-client">
    <h2>AI 对话客户端</h2>

    <!-- 配置区域 -->
    <div class="config-section">
      <label>
        提供商:
        <select v-model="config.provider">
          <option value="openai">OpenAI</option>
          <option value="gemini">Google Gemini</option>
          <option value="ollama">Ollama (本地)</option>
        </select>
      </label>

      <label v-if="config.provider !== 'ollama'">
        API 密钥:
        <input v-model="config.apiKey" type="password" placeholder="输入 API 密钥" />
      </label>

      <label>
        API 地址:
        <input
          v-model="config.baseUrl"
          :placeholder="defaultBaseUrls[config.provider]"
          type="text"
        />
      </label>

<label>
  模型名称:
  <input
    v-model="config.model"
    :placeholder="defaultModels[config.provider] || '请输入模型名'"
    type="text"
  />
</label>
    </div>

    <!-- 聊天区域 -->
    <div class="chat-container">
      <div v-for="(msg, index) in messages" :key="index" :class="`message ${msg.role}`">
        <strong>{{ msg.role === 'user' ? '👤 你' : '🤖 AI' }}:</strong>
        <div v-html="msg.content"></div>
      </div>
      <div v-if="loading" class="message ai streaming">🤖 AI: 正在思考...</div>
    </div>

    <!-- 输入区域 -->
    <div class="input-section">
      <textarea
        v-model="inputText"
        placeholder="输入你的消息..."
        :disabled="loading"
        @keydown.enter.exact.prevent="sendMessage"
        rows="3"
      ></textarea>
      <button @click="sendMessage" :disabled="loading || !inputText.trim()">
        {{ loading ? '发送中...' : '发送' }}
      </button>
    </div>

    <p class="warning" v-if="showCorsWarning">
      ⚠️ OpenAI 不允许浏览器直连。请使用 Ollama（本地）或部署 CORS 代理。
    </p>
  </div>
</template>

<script>
// 注意：不使用 import，而是动态加载 ai-sdk
export default {
  name: 'AiChatClient',
  data() {
    return {
      config: {
        provider: 'ollama',
        apiKey: '',
        baseUrl: '',
        model: ''
      },
      inputText: '',
      messages: [],
      loading: false,
      aiSdk: null,
      defaultBaseUrls: {
        openai: 'https://api.openai.com/v1',
        gemini: 'https://generativelanguage.googleapis.com/v1beta',
        ollama: 'http://localhost:11434'
      },
      defaultModels: {
        openai: 'gpt-4o',
        gemini: 'gemini-1.5-flash',
        ollama: 'llama3'
      }
    };
  },
  computed: {
    showCorsWarning() {
      return this.config.provider === 'openai' && !this.isUsingProxy();
    }
  },
  async mounted() {
    await this.loadAiSdk();
  },
  methods: {
    isUsingProxy() {
      const url = this.config.baseUrl || this.defaultBaseUrls[this.config.provider];
      return url.includes('corsproxy') || url.includes('localhost') || url.includes('127.0.0.1');
    },

    async loadAiSdk() {
      if (window.aiSdkLoaded) return;

      // 动态加载 ai-sdk（通过 esm.run CDN）
      const script = document.createElement('script');
      script.type = 'module';
      script.innerHTML = `
        import { createOpenAI, createGoogleGenerativeAI, createOllama } from 'https://esm.run/ai@latest';
        import { streamText } from 'https://esm.run/ai/core';
        window.aiProviders = { createOpenAI, createGoogleGenerativeAI, createOllama };
        window.streamText = streamText;
        window.aiSdkLoaded = true;
      `;
      document.head.appendChild(script);

      // 等待加载完成
      await new Promise((resolve) => {
        const check = () => {
          if (window.aiSdkLoaded) resolve();
          else setTimeout(check, 100);
        };
        check();
      });
    },

    async sendMessage() {
      if (!this.inputText.trim() || this.loading) return;

      const userMessage = this.inputText.trim();
      this.messages.push({ role: 'user', content: userMessage });
      this.inputText = '';
      this.loading = true;

      try {
        let providerInstance;
        const baseUrl = this.config.baseUrl || this.defaultBaseUrls[this.config.provider];
        const model = this.config.model || this.defaultModels[this.config.provider];

        if (this.config.provider === 'openai') {
          providerInstance = window.aiProviders.createOpenAI({
            apiKey: this.config.apiKey,
            baseURL: baseUrl
          });
        } else if (this.config.provider === 'gemini') {
          providerInstance = window.aiProviders.createGoogleGenerativeAI({
            apiKey: this.config.apiKey
          });
        } else if (this.config.provider === 'ollama') {
          providerInstance = window.aiProviders.createOllama({
            baseURL: baseUrl
          });
        }

        const aiMessage = { role: 'ai', content: '' };
        this.messages.push(aiMessage);

        const result = await window.streamText({
          model: providerInstance(model),
          messages: [{ role: 'user', content: userMessage }]
        });

        for await (const chunk of result.textStream) {
          aiMessage.content += chunk;
          this.$forceUpdate(); // 强制更新 DOM（因内容是字符串拼接）
        }
      } catch (error) {
        console.error('AI 请求失败:', error);
        this.messages.push({
          role: 'ai',
          content: `❌ 错误: ${error.message || '未知错误'}`
        });
      } finally {
        this.loading = false;
      }
    }
  }
};
</script>

<style scoped>
.ai-chat-client {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.config-section {
  background: #f9f9f9;
  padding: 16px;
  border-radius: 8px;
  margin-bottom: 20px;
  display: grid;
  gap: 12px;
}

.config-section label {
  display: block;
  margin-bottom: 4px;
  font-weight: bold;
}

.config-section input,
.config-section select {
  width: 100%;
  padding: 8px;
  border: 1px solid #ccc;
  border-radius: 4px;
  box-sizing: border-box;
}

.chat-container {
  border: 1px solid #ddd;
  border-radius: 8px;
  padding: 12px;
  height: 400px;
  overflow-y: auto;
  background: white;
  margin-bottom: 16px;
}

.message {
  margin-bottom: 12px;
  padding: 8px;
  border-radius: 6px;
}

.message.user {
  background: #e3f2fd;
  text-align: right;
}

.message.ai {
  background: #f1f8e9;
}

.input-section {
  display: flex;
  gap: 10px;
}

.input-section textarea {
  flex: 1;
  padding: 10px;
  border: 1px solid #ccc;
  border-radius: 4px;
}

.input-section button {
  padding: 10px 20px;
  background: #1976d2;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.input-section button:disabled {
  background: #bdbdbd;
  cursor: not-allowed;
}

.warning {
  color: #d32f2f;
  font-size: 0.9em;
  margin-top: 10px;
}
</style>
