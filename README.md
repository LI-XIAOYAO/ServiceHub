# ServiceHub
һ�����Խ��ű���������Է���ʽ���еķ��񣬸���ֶ�ִ���޷�������

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
      "Enable": true, // ״̬
      "StopIt": true, // ��ִ��ֹͣ����
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
      ]
    }
  ]

```

### ����
WIN: ���������� `ServiceHub` �������������� `NET START ServiceHub`

### ��־
WIN: Ŀǰδ���ı���־����ʹ������ `New-EventLog -LogName ServiceHub -Source ServiceHub` �����¼���־���������У��¼��鿴��>Ӧ�ó���ͷ�����־�鿴>ServiceHub

### �ƻ�
- ���ܻ�����ı���־
- ����ػ���������