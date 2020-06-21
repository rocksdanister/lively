using System;
using System.Collections.Generic;
using System.Text;

namespace livelywpf
{
    public class LibraryModel : ObservableObject
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
    }
}
