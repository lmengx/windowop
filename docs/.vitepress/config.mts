import { defineConfig } from 'vitepress'

// https://vitepress.dev/reference/site-config
export default defineConfig({
  base: '/docs/',
  title: "windowOP文档站",
  description: "介绍windowOP的使用方法",
  themeConfig: {
    // https://vitepress.dev/reference/default-theme-config
    nav: [
      { text: '首页', link: '../' },
      { text: '文档', link: '/' },
      {
        text: '相关链接',
        items: [
          { text: 'Gitee', link: 'https://gitee.com/lmx12330/window-op' },
          { text: 'Cloudflare托管', link: 'https://windowop.pages.dev' }
        ]
      },
      {
        text: '技术栈',
        items: [
          { text: 'Vue 3', link: 'https://vuejs.org/' },
          { text: 'VitePress', link: 'https://vitepress.dev/' },
          { text: 'Element Plus', link: 'https://element-plus.org/' },
          { text: '.NET 10.0', link: 'https://dotnet.microsoft.com/en-us/' }
        ]
      }
    ],

    sidebar: [
      {
        text: '介绍',
        items: [
          { text: '什么是windowOP?', link: '/introduction' },
          { text: '快速开始', link: '/quick-start' }
        ]
      },
      {
        text: '开始使用',
        items: [
          { text: '主要功能', link: '/features' },
          { text: '远程连接', link: '/remote-connection' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/vuejs/vitepress' }
    ]
  }
})
