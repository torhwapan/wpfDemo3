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
            ExecuteBtn.IsEnabled = false; // 先禁用执行按钮
            if (!ValidateAndParseSql(sql, out string whereClause, out string errorMsg, out string selectSql, out bool isComplexWhere))
            {
                MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ResultGrid.ItemsSource = null;
                SqlInput.IsReadOnly = false; // 允许继续输入
                return;
            }
            if (isComplexWhere)
            {
                MessageBox.Show($"检测到复杂WHERE条件，已原样拼接select语句，结果仅供参考！\n\n推导出的SELECT语句：{selectSql}", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            // 只支持简单的where条件：Id=1，否则全部展示
            previewData = mockData.Where(d => !isComplexWhere && whereClause == "Id=1" ? d.Id == 1 : true).Select(d => new SqlResultModel
            {
                Id = d.Id,
                Name = d.Name,
                Result = "待执行"
            }).ToList();
            ResultGrid.ItemsSource = previewData;
            ExecuteBtn.IsEnabled = previewData.Count > 0;
            SqlInput.IsReadOnly = false; // 允许继续输入
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

        // 校验和解析SQL，并推导select语句，检测复杂where
        private bool ValidateAndParseSql(string sql, out string whereClause, out string errorMsg, out string selectSql, out bool isComplexWhere)
        {
            whereClause = "";
            errorMsg = "";
            selectSql = "";
            isComplexWhere = false;
            if (string.IsNullOrWhiteSpace(sql))
            {
                errorMsg = "SQL不能为空";
                return false;
            }
            sql = sql.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(sql, @"^(update|delete)\s", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                errorMsg = "只允许UPDATE或DELETE语句";
                return false;
            }
            if (System.Text.RegularExpressions.Regex.IsMatch(sql, @"(;|--|\\b(drop|truncate|insert|alter|create)\\b)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                errorMsg = "SQL中包含非法关键字";
                return false;
            }
            var match = System.Text.RegularExpressions.Regex.Match(sql, @"where\s+(.+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                whereClause = match.Groups[1].Value.Trim();
                // 检测复杂where：包含子查询、括号、in、exists等
                if (whereClause.Contains("(") || whereClause.Contains(" in ") || whereClause.Contains(" exists ") || whereClause.Contains("select ") || whereClause.Contains("SELECT "))
                {
                    isComplexWhere = true;
                }
            }
            else
            {
                errorMsg = "必须包含WHERE条件";
                return false;
            }
            // 推导select语句
            if (sql.StartsWith("update", StringComparison.OrdinalIgnoreCase))
            {
                var tableMatch = System.Text.RegularExpressions.Regex.Match(sql, @"update\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (tableMatch.Success)
                {
                    string table = tableMatch.Groups[1].Value;
                    selectSql = $"select * from {table} where {whereClause}";
                }
            }
            else if (sql.StartsWith("delete", StringComparison.OrdinalIgnoreCase))
            {
                var tableMatch = System.Text.RegularExpressions.Regex.Match(sql, @"delete\s+from\s+(\w+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (tableMatch.Success)
                {
                    string table = tableMatch.Groups[1].Value;
                    selectSql = $"select * from {table} where {whereClause}";
                }
            }
            return true;
        }
    }
}