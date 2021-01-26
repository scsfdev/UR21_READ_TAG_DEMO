using System.Windows;
using GalaSoft.MvvmLight.Threading;

namespace UR21_READ_TAG_DEMO
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
            DispatcherHelper.Initialize();
        }
    }
}
