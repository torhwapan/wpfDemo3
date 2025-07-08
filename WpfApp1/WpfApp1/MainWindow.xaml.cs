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
            if (!ValidateAndParseSql(sql, out string whereClause, out string errorMsg))
            {
                MessageBox.Show(errorMsg, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                ResultGrid.ItemsSource = null;
                ExecuteBtn.IsEnabled = false;
                return;
            }
            // 简单模拟where解析：只支持Id=1
            previewData = mockData.Where(d => whereClause == "Id=1" ? d.Id == 1 : true).Select(d => new SqlResultModel
            {
                Id = d.Id,
                Name = d.Name,
                Result = "待执行"
            }).ToList();
            ResultGrid.ItemsSource = previewData;
            ExecuteBtn.IsEnabled = previewData.Count > 0;
        }

        private void ExecuteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (previewData == null || previewData.Count == 0)
            {
                MessageBox.Show("请先预览影响数据", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            // 执行：将结果列全部置为“成功”
            foreach (var item in previewData)
            {
                item.Result = "成功";
            }
            ResultGrid.ItemsSource = null;
            ResultGrid.ItemsSource = previewData;
        }

        private bool ValidateAndParseSql(string sql, out string whereClause, out string errorMsg)
        {
            whereClause = "";
            errorMsg = "";
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
            }
            else
            {
                errorMsg = "必须包含WHERE条件";
                return false;
            }
            return true;
        }
    }
}