<template>
  <div class="card-grid">
    <div v-for="(card, index) in cards" :key="index" class="card">
      <h3 class="card-title">{{ card.title }}</h3>
      <p class="card-content">{{ card.content }}</p>
    </div>
  </div>
</template>

<script setup>
const cards = [
  { title: '🚀 极致轻量化', content: 'windowOP安装包仅有3MB，PowerShell命令20秒即可安装' },
  { title: '💡 丰富功能', content: '独特的脚本设计，自定义你的窗口自动脚本。体验超强的文件管理' },
  { title: '🔑 加密安全', content: '使用独立加密协议，可以防止常见的中间人攻击' },
  { title: '📦 存储安全', content: 'HMAC_sha256算法加密存储，可以放心的使用你的常用密码' },
  { title: '👻 一些神奇的功能', content: '让好友的PC安装传奇全家桶、插入Z盘' },
  { title: '📝专注维护', content: '该项目正在持续更新中...' }
]
</script>

<style scoped>
/* 防溢出关键 */
.card-grid {
  display: grid;
  gap: 24px;
  padding: 20px;
  max-width: 1100px;
  margin: 0 auto;
  width: 100%;
  /* 防止内部内容意外撑开 */
  overflow: hidden;
}

/* 默认：1列 */
.card-grid {
  grid-template-columns: 1fr;
}

@media (min-width: 750px) {
  .card-grid {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (min-width: 900px) {
  .card-grid {
    grid-template-columns: repeat(3, minmax(0, 350px));
  }
}

.card {
  background: #00000007;
  border: 1px solid #eaeaea;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.05);
  max-width: 100%;
  word-wrap: break-word; /* 防止长单词溢出 */
}

.card-title {
  font-weight: bold;
  margin: 0 0 12px 0;
  font-size: 1.1em;
  color: #333;
}

.card-content {
  margin: 0;
  color: #666;
  line-height: 1.5;
}
</style>
