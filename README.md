# ServiceHub
~~一个可以将脚本、程序等以服务方式运行的服务，告别手动执行无法自启。~~  
已弃

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
      "CheckWorkingDirectory": "", // 检查工作路径
      "Enable": true, // 状态
      "StopIt": true, // 先执行停止命令
      "IsCheck": true, // 启用检查
      "CheckDaemon": 30, // 检查频率，秒
      "CheckWords": "frpc.exe", // 检查关键字
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
      ],
      "Check": [
        {
          "Command": "TASKLIST",
          "Arguments": "/FI \"IMAGENAME EQ frpc.exe\""
        }
      ]
    }
  ],
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    },
    "File": { // 文件日志
      "Formatter": "[{Category}] {DateTime:yyyy-MM-dd HH:mm:ss.fff} {LogLevel} {EventId} {ThreadId}",
      "Enable": true
    }
  },
  "Daemon": 3 // 守护进程频率，秒

```

### 运行
WIN: 服务中启动 `ServiceHub` 服务，命令行启动 `NET START ServiceHub`