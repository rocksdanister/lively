using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace livelygrid
{
    public class ViewModel : INotifyPropertyChanged
    {
        private string _title;
        private string _desc;
        private string _imagePath;
        public string Title
        {
            get
            {
                return _title;
            }
            set
            {
                _title = value;
                OnPropertyChanged("Title");
            }
        }

        public string Desc
        {
            get
            {
                return _desc;
            }
            set
            {
                _desc = value;
                OnPropertyChanged("Desc");
            }
        }

        public string ImagePath
        {
            get
            {
                return _imagePath;
            }
            set
            {
                _imagePath = value;
                OnPropertyChanged("ImagePath");
            }
        }

        private void OnPropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
