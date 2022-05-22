using Lively.Models;
using System.Threading.Tasks;

namespace Lively.UI.WinUI.Services
{
    public interface IDialogService
    {
        Task<IDisplayMonitor> ShowDisplayChooseDialog();
        Task ShowDialog(object content, string title, string message, string primaryBtnText);
        Task<DialogResult> ShowDialog(object content,
            string title,
            string primaryBtnText,
            string secondaryBtnText,
            bool isDefaultPrimary = true);
        Task<string> ShowTextInputDialog(string title);

        public enum DialogResult
        {
            none,
            primary,
            seconday
        }
    }
}