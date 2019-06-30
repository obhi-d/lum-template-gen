using Microsoft.VisualStudio.PlatformUI;

namespace LumiereObjectCreator
{
    public class BaseDialogWindow : DialogWindow
    {
        public BaseDialogWindow()
        {
            this.HasMaximizeButton = false;
            this.HasMinimizeButton = false;
        }
    }
}