# 配置管理 & 百度配置管理中心.Net客户端实现

#### 问题
1. 修改web.config导致站点重启
2. 一个大项目的不同项目部署在同一台服务器，需要修改配置时，需要逐个项目修改*.config
3. 集群部署，需要修改配置时……

#### 解决问题
1. 基于xml，剥离*.config，集中管理各种配置文件
2. 与java混搭开发，支持读取properties配置文件
3. 基于文件监控的缓存，修改即时生效
4. 支持kv、属性读取、对象、集合的映射
5. 搭配配置中心，本项目与百度配置中心disconf搭配；配置中心可以更换为其他，需自行实现
 

---

#### 总体架构图


---


#### 组件配置
在*.config中，增加自定义section，告诉组件该应用加载哪些配置文件


---

#### 组件示例

key-value方式
```
using Appsettings;
var postUrl = AppSettingsManager.AppSettings["PostUrl"];

```

直接映射成对象或对象集合

```
var person = AppSettingsManager.GetEntity<Person>();
var list = AppSettingsManager.GetEntityList<FaceMsg>("AppSettings.FaceMsgList2");

```

读取属性值

```
var name = AppSettingsManager.GetAttributesValue("Person", "Name");
var age = AppSettingsManager.GetAttributesValue("Person", "Age");
var height = AppSettingsManager.GetAttributesValue("Person", "Height");

```
更多，请参考Project Appsettings.Test

---
#### 接入disconf配置中心
1. 以管理员身份执行Install.cmd，安装Disconf.Net.WinServiceS服务
2. 配置disconf，指定disconf服务地址以及环境
3. 注册需要disconf管理的客户端配置
