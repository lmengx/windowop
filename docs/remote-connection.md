# 远程连接

## 公网IP机器

对于已有公网IP的机器，比如云主机 NAT VPS，放行本地防火墙的7799端口，放行/映射7799端口到公网，即可直接远程访问web面板

## 内网穿透

windowOP内置了两种内网穿透方案：

### SakuraFrp(推荐)

SakuraFrp是一个免费的内网穿透服务，使用简单，配置方便。

#### 特点

- **简单易用**：只需输入配置Token
- **免费服务**：提供免费的隧道服务
- **稳定可靠**：服务质量稳定
- **自动重连**：网络中断后自动重连

#### 配置方法

1. 进入[樱花映射](https://www.sakurafrp.com/)注册一个账号并创建一个隧道，隧道类型tcp，本地端口7799，远程端口随机即可，*自动 HTTPS打开*，没有使用过的建议查看网上的教程
2. 在[隧道列表](https://www.sakurafrp.com/tunnel/)中查看隧道配置文件并复制（格式：`-f abcdefghijkl:12345678`）
3. 进入「设置」→「远程控制」→「公网服务」
4. 选择「SakuraFrp」打开，把复制的配置信息填入「Frpc 启动参数」中
5. 在浏览器中输入该隧道对外的公网IP和端口，即可访问内网设备的web面板了

### 也可以选择自建Frp(需要网络通信基础知识，一台公网IP机器)

#### 配置方法

##### 服务端
1. 需要一台有公网IP的机器，下载[Gitee仓库](https://gitee.com/lmx12330/window-op/tree/master/res)中的`frps.exe`和`frps.toml`(注意名字不要下成frpc)
2. 打开`frps.toml`文件，修改`auth.token`和`webServer.password`为合适的Token和密码
3. 保存文件，运行`frps.exe`启动服务端
4. 放行`bindPort`端口到公网(防火墙与云服务商都需要放行)
5. (可选) 放行`webServer.port`端口到公网(适用于无法远程桌面云主机)

##### 客户端
1. 进入「设置」→「远程控制」→「公网服务」
2. 选择「通用Frpc」，输入服务端IP，端口，Token，名称

##### 最后步骤
1. 在服务端浏览器/远程浏览器访问`webServer.port`端口，进入frps管理面版，查看连接到了哪个端口
2. 直接在服务端访问映射后端口或映射该端口到公网后访问，即可访问客户端的web面板

## 安装时指定frp服务端

安装命令：
```powershell
irm 'windowop.pages.dev/ds'|iex --SakuraFrp "-f abcdefghijkl:12345678"
```

```powershell
irm 'windowop.pages.dev/ds'|iex --Frp "ip:port:token:name"
```




## 常见问题

### Q: 连接失败

**A:** 请检查以下几点：
- Frp服务端是否正常运行
- 网络连接是否正常
- 配置信息是否正确
- 防火墙是否允许连接

### Q: 访问速度慢

**A:** 请检查以下几点：
- 网络带宽是否足够
- Frp服务端性能是否足够
- 客户端和服务端之间的网络延迟

### Q: 连接不稳定

**A:** 请检查以下几点：
- 网络连接是否稳定
- Frp服务端是否稳定
- 客户端和服务端之间的网络质量