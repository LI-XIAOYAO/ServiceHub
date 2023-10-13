# ServiceHub
~~һ�����Խ��ű���������Է���ʽ���еķ��񣬸���ֶ�ִ���޷�������~~  
����

### ��װ
WIN: ִ�� `Install.bat` �ű���������

### ����
`appsetting.json` �����`Config`�ڵ�
�����
``` json
  "Config": [
    {
      "Name": "Frp", // ����
      "StartWorkingDirectory": "D:/frp/", // ��������·��
      "StopWorkingDirectory": "", // ֹͣ����·��
      "CheckWorkingDirectory": "", // ��鹤��·��
      "Enable": true, // ״̬
      "StopIt": true, // ��ִ��ֹͣ����
      "IsCheck": true, // ���ü��
      "CheckDaemon": 30, // ���Ƶ�ʣ���
      "CheckWords": "frpc.exe", // ���ؼ���
      "Start": [ // ����ִ�У������ö������������ϳɹ�������
        {
          "Command": "frpc.exe", // ����
          "Arguments": "-c frpc-service.ini" // ����
        }
      ],
      "Stop": [ // ִֹͣ�У������ö�������������������
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
    "File": { // �ļ���־
      "Formatter": "[{Category}] {DateTime:yyyy-MM-dd HH:mm:ss.fff} {LogLevel} {EventId} {ThreadId}",
      "Enable": true
    }
  },
  "Daemon": 3 // �ػ�����Ƶ�ʣ���

```

### ����
WIN: ���������� `ServiceHub` �������������� `NET START ServiceHub`