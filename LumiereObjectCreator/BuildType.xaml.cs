using EnvDTE;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LumiereObjectCreator
{
    /// <summary>
    /// Interaction logic for BuildType.xaml
    /// </summary>
    public partial class BuildType
    {
        List<BuildConfig> configs;
        OutputWindowPane outputPane;
        internal BuildType(List<BuildConfig> buildConfigs, OutputWindowPane outputPane)
        {
            InitializeComponent();
            this.txtType.ItemsSource = ToStringList(buildConfigs);
            this.configs = buildConfigs;
            this.outputPane = outputPane;
        }
        
        private void WindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private List<string> ToStringList(List<BuildConfig> buildConfigs)
        {
            List<string> ls = new List<string>();
            foreach (var cfg in buildConfigs)
                ls.Add(cfg.Name);
            return ls;
        }
        public void OnKeyDownHandler(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                var name = this.txtType.SelectedValue.ToString();
                foreach (var cfg in configs)
                {
                    if (name == cfg.Name)
                    {
                        cfg.Build(this.outputPane);
                        break;
                    }
                }
            }
            else if (e.Key == Key.Escape)
                this.Close();
        }

        public void OnRun(object sender)
        {
            var name = this.txtType.SelectedValue.ToString();
            foreach (var cfg in configs)
            {
                if (name == cfg.Name)
                {
                    cfg.Build(this.outputPane);
                    this.Close();
                }
            }
            this.Close();
        }

        public void OnCancel(object sender)
        {
            this.Close();
        }
    }
}
