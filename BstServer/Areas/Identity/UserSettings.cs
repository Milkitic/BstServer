using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace BstServer.Areas.Identity;

public class UserSettings
{
    public string UserId { get; set; }
    public FileSettings File { get; set; } = new FileSettings();
    public UploadViewModel Upload { get; set; } = new UploadViewModel();

    public class FileSettings
    {
        [Display(Name = "显示全部文件")]
        public bool DisplayAll { get; set; } = false;
    }

    public class UploadViewModel
    {
        [Required] [Display(Name = "备注")]
        public string Remark { get; set; }

        [Required]
        [Display(Name = "mod文件")]
        [FileExtensions(Extensions = ".jar,.zip", ErrorMessage = "上传格式错误")]
        public IFormFile AddonFile { get; set; }
    }
}