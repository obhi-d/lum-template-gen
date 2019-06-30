using System.Collections.Generic;
using System.IO;
using System.Windows.Input;

namespace LumiereObjectCreator
{
    /// <summary>
    /// Interaction logic for Input.xaml
    /// </summary>
    public partial class Input
    {
        public string result = "";
        public string objectType = "Auto";

        public Input(string scanPath)
        {
            InitializeComponent();
            string[] source = Directory.GetDirectories(scanPath);
            List<string> sourceList = new List<string>();
            sourceList.Add("Auto");
            foreach (var dir in source)
            {
                sourceList.Add(dir.Substring(dir.LastIndexOf(Path.DirectorySeparatorChar) + 1));
            }
            this.txtType.ItemsSource = sourceList;
        }

        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        public void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                result = this.txtAnswer.Text;
                objectType = this.txtType.SelectedValue.ToString();
                this.Close();
            }
            else if (e.Key == Key.Escape)
                this.Close();
        }

        public bool GetResult(out string oType, out string oName)
        {
            oType = objectType;
            oName = result;
            return result.Length > 0 ? true : false;
        }
    }
}