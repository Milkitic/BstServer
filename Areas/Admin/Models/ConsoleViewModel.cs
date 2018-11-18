using Milkitic.ApplicationHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BstServer.Areas.Admin.Models
{
    public class ConsoleViewModel
    {
        public ConsoleViewModel()
        {

        }

        public ConsoleViewModel(L4D2AppHost appHost)
        {
            Args = appHost.Args;
            CanSendMessage = appHost.CanSendMessage;
            FileName = appHost.FileName;
            this.Guid = appHost.Guid;
            HostSettings = new HostSettingsCopy(appHost.HostSettings);
            IsRunning = appHost.IsRunning ?? false;
            RunningGuid = appHost.RunningGuid;
        }

        [Display(Name ="运行实例 GUID")]
        public Guid RunningGuid { get; set; }

        [Display(Name = "进程名")]
        public string FileName { get; set; }

        [Display(Name = "重定向输入")]
        public bool CanSendMessage { get; set; }

        [Display(Name = "启动参数")]
        public string Args { get; set; }

        [Display(Name = "实例 GUID")]
        public Guid Guid { get; set; }

        [Display(Name = "运行状况")]
        public bool IsRunning { get; set; }

        public HostSettingsCopy HostSettings { get; set; }

        public class HostSettingsCopy
        {
            public HostSettingsCopy()
            {

            }

            public HostSettingsCopy(HostSettings hostSettings)
            {
                ShowWindow = hostSettings.ShowWindow;
                RedirectStandardInput = hostSettings.RedirectStandardInput;
                //Encoding = hostSettings.Encoding;
            }

            [Display(Name = "后台显示窗口，不作为宿主程序")]
            public bool ShowWindow { get; set; } = false;
            [Display(Name = "开启重定向输入")]
            public bool RedirectStandardInput { get; set; } = false;
            //[Display(Name = "编码")]
            //public Encoding Encoding { get; set; } = Encoding.UTF8;
        }

    }
}
