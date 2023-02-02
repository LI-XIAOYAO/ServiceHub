# ServiceHub
一个可以将脚本、程序等以服务方式运行的服务，告别手动执行无法自启。

### 安装
WIN: 执行 `Install.bat` 脚本创建服务

### 配置
`appsetting.json` 中添加`Config`节点
配置项：
``` json
  "Config": [
    {
      "Name": "Frp", // 名称
      "StartWorkingDirectory": "D:/frp/", // 启动工作路径
      "StopWorkingDirectory": "", // 停止工作路径
      "Enable": true, // 状态
      "StopIt": true, // 先执行停止命令
      "Start": [ // 启动执行，可配置多个命令具有向上成功依赖性
        {
          "Command": "frpc.exe", // 命令
          "Arguments": "-c frpc-service.ini" // 参数
        }
      ],
      "Stop": [ // 停止执行，可配置多个命令具有向上依赖性
        {
          "Command": "TASKKILL",
          "Arguments": "/F /IM frpc.exe"
        }
      ]
    }
  ]

```

### 运行
WIN: 服务中启动 `ServiceHub` 服务，命令行启动 `NET START ServiceHub`

### 日志
WIN: 目前未加文本日志，可使用命令 `New-EventLog -LogName ServiceHub -Source ServiceHub` 创建事件日志，管理工具中：事件查看器>应用程序和服务日志查看>ServiceHub

### 计划
- 可能会加入文本日志
- 添加守护进程配置