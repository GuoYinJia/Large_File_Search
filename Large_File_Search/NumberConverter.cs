using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace 大文件搜索
{
    /// <summary>
    /// 对数字进行转换
    /// </summary>
    class NumberConverter : IValueConverter
    {
        /// <summary>
        /// 根据值大小返回不同的表达形式，比如KB，MB，GB
        /// 大的数字用逗号作为千分位间隔符
        /// </summary>
        /// <param name="value"></param>
        /// <param name="targetType"></param>
        /// <param name="parameter"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            long longValue = (long)value;

            double temp = 0;
            //大于1k,小于1M
            if ((longValue >= 1024) && (longValue < 1024 * 1024))
            {
                temp = (double)longValue / 1024;
                return temp.ToString("N") + "K";
            }
            //大于1M，小于1G
            if ((longValue >= 1024 * 1024) && (longValue < 1024 * 1024 * 1024))
            {
                temp = (double)longValue / (1024 * 1024);
                return temp.ToString("N") + "M";
            }
            //大于1G
            if ((longValue >= 1024 * 1024 * 1024))
            {
                temp = (double)longValue / (1024 * 1024 * 1024);
                return temp.ToString("N") + "G";
            }
            //小于1K的，返回真实大小
            return longValue.ToString() + "字节";
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
