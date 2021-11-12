using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace MiscTools
{
    public partial class MiscToolsSettingsView : UserControl
    {
        // If this regex matches something, the text is not a valid number
        private readonly Regex numberRegex = new Regex("[^0-9]+", RegexOptions.Compiled);

        public MiscToolsSettingsView()
        {
            InitializeComponent();
        }

        private bool IsNumber(string text)
        {
            return !numberRegex.IsMatch(text);
        }

        private void txtLargeMediaThreshold_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !IsNumber(e.Text);
        }

        private void txtLargeMediaThreshold_Pasting(object sender, System.Windows.DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));

                if (!IsNumber(text))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}