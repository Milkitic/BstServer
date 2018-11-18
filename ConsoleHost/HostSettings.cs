using System.Text;

namespace Milkitic.ApplicationHost
{
    public class HostSettings
    {
        public HostSettings()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public bool ShowWindow { get; set; } = false;
        public bool RedirectStandardInput { get; set; } = false;
        public Encoding Encoding { get; set; } = Encoding.UTF8;
    }
}