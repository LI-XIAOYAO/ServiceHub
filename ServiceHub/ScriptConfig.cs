using System.Collections.ObjectModel;
using static ServiceHub.ScriptConfig;

namespace ServiceHub
{
    /// <summary>
    /// 脚本配置
    /// </summary>
    public class ScriptConfig
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 启动工作路径
        /// </summary>
        public string StartWorkingDirectory { get; set; }

        /// <summary>
        /// 停止工作路径
        /// </summary>
        public string StopWorkingDirectory { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public bool Enable { get; set; }

        /// <summary>
        /// 先执行停止命令
        /// </summary>
        public bool StopIt { get; set; }

        /// <summary>
        /// 启动执行
        /// </summary>
        public CommandConfigs Start { get; set; } = new CommandConfigs(CommandOptions.Start);

        /// <summary>
        /// 停止执行
        /// </summary>
        public CommandConfigs Stop { get; set; } = new CommandConfigs(CommandOptions.Stop);

        /// <summary>
        /// 命令配置
        /// </summary>
        public class CommandConfig
        {
            /// <summary>
            /// 执行命令选项
            /// </summary>
            public CommandOptions CommandOptions { get; internal set; }

            /// <summary>
            /// 命令
            /// </summary>
            public string Command { get; set; }

            /// <summary>
            /// 参数
            /// </summary>
            public string Arguments { get; set; }

            public override string ToString()
            {
                return string.Concat(Command, " ", Arguments);
            }
        }
    }

    /// <summary>
    /// 配置
    /// </summary>
    public class CommandConfigs : Collection<CommandConfig>
    {
        public CommandConfigs(CommandOptions commandOptions)
        {
            CommandOptions = commandOptions;
        }

        /// <summary>
        /// 执行命令选项
        /// </summary>
        public CommandOptions CommandOptions { get; set; }

        protected override void InsertItem(int index, CommandConfig item)
        {
            item.CommandOptions = CommandOptions;
            base.InsertItem(index, item);
        }
    }
}