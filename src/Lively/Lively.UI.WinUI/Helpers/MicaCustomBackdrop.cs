using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace Lively.UI.WinUI.Helpers
{
    //refs:
    //https://github.com/dotMorten/WinUIEx/blob/main/src/WinUIEx/SystemBackdrop.cs
    //https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.systembackdrop?view=windows-app-sdk-1.3
    public class MicaCustomBackdrop : SystemBackdrop
    {
        MicaController _micaController;

        public MicaCustomBackdrop()
        {
            //TODO
        }

        protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            base.OnTargetConnected(connectedTarget, xamlRoot);

            if (_micaController != null)
            {
                throw new Exception("This controller can't be shared");
            }

            SetControllerConfig(connectedTarget, xamlRoot);

            _micaController = new MicaController();
            _micaController.AddSystemBackdropTarget(connectedTarget);
        }

        protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
        {
            base.OnTargetDisconnected(disconnectedTarget);

            _micaController.RemoveSystemBackdropTarget(disconnectedTarget);
            _micaController = null;
        }

        void SetControllerConfig(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
        {
            var config = GetDefaultSystemBackdropConfiguration(connectedTarget, xamlRoot);
            _micaController.SetSystemBackdropConfiguration(config);
        }
    }
}
