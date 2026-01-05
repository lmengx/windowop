<template>
  <div class="container">
    <!-- 响应式主区域 -->
    <div
      class="content-wrapper"
      :style="{
        display: 'flex',
        flexDirection: windowWidth < 960 ? 'column' : 'row',
        gap: '40px',
        alignItems: windowWidth < 960 ? 'stretch' : 'center'
      }"
    >
      <!-- 图片放前面 → 小屏时在上方 -->
      <div class="image">
        <img :src="impression" alt="windowOP 演示图" class="hero-image" />
      </div>

      <!-- 文字内容 -->
      <div class="main">
        <div class="title-group">
          <span class="super-size gradient-text">windowOP</span>
          <span class="super-size">极速部署的</span>
          <span class="super-size">远程控制软件</span>
          <p class="intro-text">
            一行命令即可安装，一个地址即可连接。<br />
            完全部署于被控端，强加密。
          </p>
        </div>

        <div class="button-group">
          <el-button size="large" color="#626aef" round @click="openDocs">快速开始</el-button>
          <el-button size="large" color="#ebebef" round @click="connect">立即连接</el-button>
          <el-button size="large" round text @click="openBili">
            <el-icon><IEpVideoPlay /></el-icon>
            观看演示视频
          </el-button>

        </div>
      </div><!-- main -->
    </div><!-- 响应式主区域 -->

    <div class="card-group">

      <cards />
    </div>


  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import { useWindowWidth } from '@/composables/useWindowWidth'
import impression from '@/assets/impression.svg'

import cards from './HomeView-cards.vue'

const { windowWidth } = useWindowWidth()
const router = useRouter()

function openDocs(){
  window.open('https://flowus.cn/lmx12330/share/0d2b743d-9fa2-4a7e-9958-c1b0b4050ccc', '_blank')
}

function openBili(){
  window.open('https://www.bilibili.com/video/BV1RJ6oYoEKC/?spm_id_from=333.1387.homepage.video_card.click', '_blank')
}

function connect()
{
  router.push('/connect')
}

</script>

<style scoped>
/* 最外层容器：整体居中 */
.container {
  padding: 2rem;
  max-width: 1200px;       /* 限制最大宽度 */
  margin: 0 auto;          /* 水平居中 */
  width: 100%;
}

/* 内容区：默认不设宽高，由子元素撑开 */

/* 标题样式 */
.super-size {
  font-size: 3rem;
  font-weight: 700;
  line-height: 1;
  margin: 0;
  padding: 0;
}

.gradient-text {
  background: linear-gradient(80deg, #626aef, #ffffff);
  -webkit-background-clip: text;
  background-clip: text;
  -webkit-text-fill-color: transparent;
}

.title-group {
  display: flex;
  flex-direction: column;
  gap: 5px;
}

.intro-text {
  margin-top: 15px;
  font-size: 1.5rem;
  color: #666;
  line-height: 1.4;
}

.button-group {
  margin-top: 30px;
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

/* 默认：小屏（<960px） */
.hero-image {
  width: 200px;        /* 固定 200px */
  height: auto;
  display: block;
  margin: 0 auto;
}

/* 中屏：960px ~ 1299px —— 线性缩放 */
@media (min-width: 960px) and (max-width: 1299px) {
  .hero-image {
    /* 使用 calc() 实现线性插值：
       当 width = 960 → 200px
       当 width = 1300 → 500px
       每 1px 屏幕宽度增加 (500-200)/(1300-960) ≈ 0.882px 图片宽度
    */
    width: calc(200px + (100vw - 960px) * (300 / 340));
    max-width: 500px;
    height: auto;
    margin: 0; /* 大屏不居中 */
  }
}

/* 大屏：≥1300px */
@media (min-width: 1300px) {
  .hero-image {
    width: 500px;
    height: auto;
    margin: 0;
  }
}

/* 在宽度小于960时给main框内容水平居中的属性 */
@media (max-width: 960px) {
  .main,.button-group, .title-group{
    text-align: center; /* 对文本内容进行居中 */
  }
    .button-group, .title-group {
    width: 100%; /* 确保它们占据整个行宽 */
    box-sizing: border-box; /* 确保padding和border包含在width之内 */
  }

  .button-group {
    justify-content: center; /* 如果需要垂直居中，可以在父元素上添加 */
  }
}

.card{

}
</style>
