using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // 模拟数据
        private List<SqlResultModel> mockData = new List<SqlResultModel>
        {
            new SqlResultModel { Id = 1, Name = "张三" },
            new SqlResultModel { Id = 2, Name = "李四" },
            new SqlResultModel { Id = 3, Name = "王五" }
        };
        private List<SqlResultModel> previewData = new List<SqlResultModel>();

        private void PreviewBtn_Click(object sender, RoutedEventArgs e)
        {
            string sql = SqlInput.Text.Trim();
            // 允许以分号结尾，去掉尾部分号
            if (sql.EndsWith(";")) sql = sql.Substring(0, sql.Length - 1).TrimEnd();
            ExecuteBtn.IsEnabled = false; // 先禁用执行按钮
            if (!IsSafeSql(sql, out string errorMsg))
            {
                MessageBox.Show(errorMsg, "SQL防呆校验失败", MessageBoxButton.OK, MessageBoxImage.Error);
                ResultGrid.ItemsSource = null;
                SqlInput.IsReadOnly = false;
                return;
            }
            if (!ConvertToSelect(sql, out string whereClause, out string selectSql, out bool isComplexWhere, out string convertMsg))
            {
                MessageBox.Show(convertMsg, "SQL解析失败", MessageBoxButton.OK, MessageBoxImage.Error);
                ResultGrid.ItemsSource = null;
                SqlInput.IsReadOnly = false;
                return;
            }
            if (!string.IsNullOrEmpty(convertMsg))
            {
                MessageBox.Show(convertMsg + $"\n\n推导出的SELECT语句：{selectSql}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            // 只支持简单的where条件：Id=1，否则全部展示
            previewData = mockData.Where(d => !isComplexWhere && whereClause == "Id=1" ? d.Id == 1 : true).Select(d => new SqlResultModel
            {
                Id = d.Id,
                Name = d.Name,
                Result = "待执行"
            }).ToList();
            int total = previewData.Count;
            if (total > 20)
            {
                MessageBox.Show($"影响数据共{total}条，仅展示前20条！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                previewData = previewData.Take(20).ToList();
            }
            ResultGrid.ItemsSource = previewData;
            ExecuteBtn.IsEnabled = previewData.Count > 0;
            SqlInput.IsReadOnly = false;
        }

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (previewData == null || previewData.Count == 0)
            {
                MessageBox.Show("请先预览影响数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            foreach (var item in previewData)
            {
                item.Result = "成功";
            }
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = previewData;
        }

        // AI增强：update/delete转select，兼容多种写法
        private bool ConvertToSelect(string sql, out string whereClause, out string selectSql, out bool isComplexWhere, out string msg)
        {
            whereClause = "";
            selectSql = "";
            isComplexWhere = false;
            msg = "";
            try
            {
                // 只允许update/delete开头
                var sqlLower = sql.ToLower();
                if (!(sqlLower.StartsWith("update ") || sqlLower.StartsWith("delete ")))
                {
                    msg = "只允许UPDATE或DELETE语句";
                    return false;
                }
                // 提取where条件
                var whereMatch = Regex.Match(sql, @"where\s+(.+)", RegexOptions.IgnoreCase);
                if (!whereMatch.Success)
                {
                    msg = "必须包含WHERE条件";
                    return false;
                }
                whereClause = whereMatch.Groups[1].Value.Trim();
                // 检测复杂where
                if (whereClause.Contains("(") || whereClause.Contains(" in ") || whereClause.Contains(" exists ") || whereClause.Contains("select ") || whereClause.Contains("SELECT ") || whereClause.Contains(" or ") || whereClause.Contains(" and "))
                {
                    isComplexWhere = true;
                    msg = "检测到复杂WHERE条件，select语句仅供参考！";
                }
                // 提取表名（兼容别名、hint、with、delete from ...等）
                string table = "";
                if (sqlLower.StartsWith("update "))
                {
                    // update table [alias] set ...
                    var match = Regex.Match(sql, @"update\s+([\w\.]+)(?:\s+\w+)?\s+set", RegexOptions.IgnoreCase);
                    if (match.Success)
                        table = match.Groups[1].Value;
                }
                else if (sqlLower.StartsWith("delete "))
                {
                    // delete from table [alias] where ...
                    var match = Regex.Match(sql, @"delete\s+from\s+([\w\.]+)(?:\s+\w+)?", RegexOptions.IgnoreCase);
                    if (match.Success)
                        table = match.Groups[1].Value;
                }
                if (string.IsNullOrEmpty(table))
                {
                    msg = "未能识别表名，无法转换为SELECT语句，请检查SQL格式！";
                    return false;
                }
                selectSql = $"select * from {table} where {whereClause}";
                return true;
            }
            catch (Exception ex)
            {
                msg = "SQL解析异常：" + ex.Message;
                return false;
            }
        }

        // AI增强：SQL防呆校验
        private bool IsSafeSql(string sql, out string errorMsg)
        {
            errorMsg = "";
            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMsg = "SQL不能为空";
                return false;
            }
            // 只允许update/delete开头
            if (!Regex.IsMatch(sql, @"^(update|delete)\s", RegexOptions.IgnoreCase))
            {
                errorMsg = "只允许UPDATE或DELETE语句";
                return false;
            }
            // 禁止多条SQL
            if (sql.Count(c => c == ';') > 0 && !sql.TrimEnd().EndsWith(";"))
            {
                errorMsg = "不允许多条SQL语句";
                return false;
            }
            // 禁止危险关键字
            string[] dangerWords = { "drop", "truncate", "alter", "insert", "create", "exec", "execute", "merge", "call", "grant", "revoke", "backup", "restore", "replace", "union", "intersect", "minus", "load", "outfile", "dual" };
            foreach (var word in dangerWords)
            {
                if (Regex.IsMatch(sql, $@"\\b{word}\\b", RegexOptions.IgnoreCase))
                {
                    errorMsg = $"SQL中包含非法关键字：{word}";
                    return false;
                }
            }
            // 禁止注释
            if (sql.Contains("--") || sql.Contains("/*") || sql.Contains("*/"))
            {
                errorMsg = "SQL中包含注释符号";
                return false;
            }
            // 禁止拼接变量
            if (Regex.IsMatch(sql, @"[@#:][\w]+|\$\{[\w]+\}", RegexOptions.IgnoreCase))
            {
                errorMsg = "SQL中包含变量拼接，存在注入风险";
                return false;
            }
            // 必须有where
            if (!Regex.IsMatch(sql, @"where\s+.+", RegexOptions.IgnoreCase))
            {
                errorMsg = "必须包含WHERE条件";
                return false;
            }
            // 禁止全表操作
            if (Regex.IsMatch(sql, @"where\s+1\s*=\s*1|or\s+1\s*=\s*1", RegexOptions.IgnoreCase))
            {
                errorMsg = "WHERE条件存在全表操作风险";
                return false;
            }
            // 禁止select * from dual
            if (Regex.IsMatch(sql, @"select\s+\*\s+from\s+dual", RegexOptions.IgnoreCase))
            {
                errorMsg = "SQL中包含非法绕过技巧";
                return false;
            }
            return true;
        }
    }
}