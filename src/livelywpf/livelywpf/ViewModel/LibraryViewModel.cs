using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace livelywpf
{
    public class LibraryViewModel : ObservableObject
    {
        public LibraryViewModel()
        {
            for (int i = 0; i < 15; i++)
            {
                LibraryItems.Add(new LibraryModel() { Title = i.ToString(), Desc = "a wallpaper that is cool", ImagePath = @"C:\Users\rocks\Documents\GIFS\lively.gif" });
            }
        }

        private ObservableCollection<LibraryModel> _libraryItems = new ObservableCollection<LibraryModel>();
        public ObservableCollection<LibraryModel> LibraryItems
        {
            get { return _libraryItems; }
            set
            {
                if (value != _libraryItems)
                {
                    _libraryItems = value;
                    OnPropertyChanged("LibraryItems");
                }
            }
        }
    }
}
