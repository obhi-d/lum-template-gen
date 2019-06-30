using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;

namespace LumiereObjectCreator
{
    public class Options : DialogPage
    {
        private String author;
        private String email;
        private String templates;
        public const string Key = "LumTemplateSettings";

        [Category("General")]
        [DisplayName("Author")]
        [Description("Author name")]
        public string Author { get; set; }

        [Category("General")]
        [DisplayName("Email")]
        [Description("Author email.")]
        public string Email { get; set; }

        [Category("General")]
        [DisplayName("Templates location")]
        [Description("Location for templates folder.")]
        public string TemplateLocation { get; set; } = "Templates";

        [Category("General")]
        [DisplayName("Conversion rules file")]
        [Description("Location for conversion rule JSON file.")]
        public string RulesLocation { get; set; } = "namespace.json";

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);
        }
    }
}