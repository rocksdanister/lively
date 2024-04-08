using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lively.Services
{
    public interface IDialogService
    {
        public void ShowErrorDialog(string title, string message);
    }
}
