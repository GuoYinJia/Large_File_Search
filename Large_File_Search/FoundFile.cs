using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 大文件搜索
{
    /// <summary>
    /// 代表找到一个文件
    /// </summary>
    class FoundFile : INotifyPropertyChanged
    {
        private string name;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get { return name; }
            set 
            {
                if (name != value)
                {
                    OnPropertyChanged("Name");
                    name = value;
                }
            }
        }

        private long length;

        /// <summary>
        /// 大小
        /// </summary>
        public long Length
        {
            get { return length; }
            set
            {
                if (length != value)
                {
                    OnPropertyChanged("Length");
                    length = value;
                }
            }
        }

        private string location;

        /// <summary>
        /// 位置
        /// </summary>
        public string Location
        {
            get { return location; }
            set
            {
                if (location != value)
                {
                    OnPropertyChanged("Location");
                    location = value;
                }
            }
        }

        //事件委托
        public event PropertyChangedEventHandler PropertyChanged;
       
        /// <summary>
        /// 改变资源时发生
        /// </summary>
        /// <param name="name"></param>
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
