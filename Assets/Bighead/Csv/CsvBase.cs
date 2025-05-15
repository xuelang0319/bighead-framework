namespace Bighead.Csv
{
    /// <summary>
    /// 配置表架构
    /// </summary>
    public abstract class CsvBase
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        protected virtual string FolderPath { get; set; }

        /// <summary>
        /// 表单名称
        /// </summary>
        protected virtual string SheetName { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public abstract void Initialize();

        /// <summary>
        /// 反初始化
        /// </summary>
        public abstract void Terminate();
    }
}