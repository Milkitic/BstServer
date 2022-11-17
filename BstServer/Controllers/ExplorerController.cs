using BstServer.Areas.Admin;
using BstServer.Areas.Admin.Models;
using BstServer.Areas.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Milkitic.FileExplorer;
using Milkitic.FileExplorer.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BstServer.Controllers;

[Area("admin")]
[Route("admin/explorer")]
[Authorize]
public class ExplorerController : Controller
{
    private readonly Explorer _explorer;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly UserSettingsManager _userSettingsManager;
    private readonly IHostingEnvironment _env;

    private const string SessionKeyCurrentFolder = "CurrentFolder";

    public ExplorerController(Explorer explorer, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, UserSettingsManager userSettingsManager,
        IHostingEnvironment env)
    {
        _explorer = explorer;
        _signInManager = signInManager;
        _userManager = userManager;
        _userSettingsManager = userSettingsManager;
        _env = env;
    }

    [Route("")]
    public ActionResult Index()
    {
        if (TempData.ContainsKey("Message"))
            ViewBag.Message = TempData["Message"];
        if (_userSettingsManager.TryGetSettings(_userManager.GetUserId(User), out var settings))
        {
            return View(settings);
        }

        return View();
    }

    [Route("~/api/change_settings")]
    [HttpPost]
    public ActionResult ChangeSettings(UserSettings fileSettings)
    {
        if (_userSettingsManager.TryGetSettings(_userManager.GetUserId(User), out var settings))
        {
            settings.File = fileSettings.File;
            _userSettingsManager.SaveToFile();
        }

        return RedirectToAction("Index");
    }

    [Route("~/api/get_files")]
    [HttpPost]
    public ActionResult GetFileList(string subFolder)
    {
        if (_userSettingsManager.TryGetSettings(_userManager.GetUserId(User), out var settings))
        {
            if (!settings.File.DisplayAll)
                _explorer.DisplayFilter = new[] { "cfg", "vpk", "txt", "smx", "_disabled" };
            _explorer.Refresh();
        }

        if (subFolder == "!")
        {
            _explorer.NavigateToUpload();
        }
        else
        {
            NavigateToLastFolder();
            if (!string.IsNullOrEmpty(subFolder))
            {
                try
                {
                    if (subFolder != "..")
                        _explorer.NavigateSubFolder(subFolder);
                    else
                        _explorer.NavigateParentFolder();
                }
                catch (UnauthorizedAccessException e)
                {
                    return Json(new JsonModelBase(403, "failed", e.Message, null));
                }
                catch (Exception e)
                {
                    if (e is DirectoryNotFoundException || e is FileNotFoundException)
                    {
                        return Json(new JsonModelBase(404, "failed", e.Message, null));
                    }

                    return Json(new JsonModelBase(500, "failed", e.Message, null));
                }
            }
        }
        HttpContext.Session.SetString(SessionKeyCurrentFolder, _explorer.CurrentPathText);

        return Json(new JsonModelBase(200, "success", "success",
            new ExplorerJsonModel(_explorer.CurrentPathText, _explorer.IsExchangePath, _explorer.GetViewCells().ToList())));
    }

    [Route("~/api/get_file")]
    [HttpPost]
    public ActionResult GetFile(string fileName = null, bool getContent = true)
    {
        try
        {
            NavigateToLastFolder();
            FileCell file = _explorer.SearchChildFile(fileName);
            string content;
            //if (getContent)
            Explorer.TryGetContent(file.FullPath, out content);
            //else
            //    content = "文件过大，不给予显示。";
            var iconPath = Path.Combine(_env.WebRootPath, "images", "icons", file.Extension + ".png");
            if (!System.IO.File.Exists(iconPath))
                file.FileIcon.Save(iconPath);
            return Json(new JsonModelBase(200, "success", "success", new { detail = file, content }));
        }
        catch (FileNotFoundException e)
        {
            return Json(new JsonModelBase(404, "failed", e.Message, null));
        }
        catch (Exception e)
        {
            return Json(new JsonModelBase(500, "failed", e.Message, null));
        }
    }

    [Route("~/api/save_file")]
    [HttpPost]
    public ActionResult SaveFile(string fileName, string content)
    {
        try
        {
            NavigateToLastFolder();
            _explorer.SaveContent(fileName, content);
            return Json(new JsonModelBase(200, "success", "success", null));
        }
        catch (InvalidOperationException e)
        {
            return Json(new JsonModelBase(403, "failed", e.Message, null));
        }
        catch (Exception e)
        {
            return Json(new JsonModelBase(500, "failed", e.Message, null));
        }
    }

    [Route("~/api/change_file_status")]
    [HttpPost]
    public ActionResult ChangeFileStatus(string fileName)
    {
        try
        {
            NavigateToLastFolder();
            FileCell file = _explorer.SearchChildFile(fileName);
            if (file.IsEnabled)
                _explorer.DisableFile(file.Name);
            else
                _explorer.EnableFile(file.Name);
            return Json(new JsonModelBase(200, "success", "success", null));
        }
        catch (InvalidOperationException e)
        {
            return Json(new JsonModelBase(403, "failed", e.Message, null));
        }
        catch (Exception e)
        {
            return Json(new JsonModelBase(500, "failed", e.Message, null));
        }
    }

    [HttpPost]
    [RequestSizeLimit(long.MaxValue)]
    [Route("~/api/delete_file")]
    public ActionResult DeleteFile(string fileName)
    {
        try
        {
            NavigateToLastFolder();
            _explorer.DeleteFile(fileName);

            return Json(new JsonModelBase(200, "success", "success", null));
        }
        catch (InvalidOperationException e)
        {
            return Json(new JsonModelBase(403, "failed", e.Message, null));
        }
        catch (UnauthorizedAccessException e)
        {
            return Json(new JsonModelBase(403, "failed", e.Message, null));
        }
        catch (Exception e)
        {
            return Json(new JsonModelBase(500, "failed", e.Message, null));
        }
    }

    [HttpPost]
    [RequestSizeLimit(long.MaxValue)]
    [Route("~/api/upload_file")]
    public IActionResult Upload(UserSettings settings)
    {
        var file = settings.Upload;
        string status = "上传失败";

        try
        {
            if (ModelState.IsValid)
            {
                throw new Exception("提交的数据不正确。");
            }

            string[] supportedExt = { "vpk", "zip" };
            string ext = file.AddonFile.FileName.Split('.').Last().ToLower();
            if (!supportedExt.Contains(ext))
            {
                throw new NotSupportedException($"File format \".{ext}\" is not supported.");
            }

            var fileName = string.IsNullOrEmpty(file.Remark?.Trim())
                ? file.AddonFile.FileName
                : $"{file.Remark}.{ext}";
            char[] reverseChar = { '/', '\\', ':', ',', '*', '?', '\'', '<', '>', '|' };

            if (fileName.Any(c => c > 127 && c < 32) || fileName.Any(c => reverseChar.Contains(c)))
            {
                throw new NotSupportedException("File name is invalid. Should be characters or system-support path symbols.");
            }

            _explorer.NavigateToUpload();
            int i = 2;
            var array = fileName.Split('.');
            while (_explorer.ViewCells.Any(k => k.Name == fileName))
            {
                string tmpName = string.Join('.', array.Take(array.Length - 1)),
                    tmpExt = array.Last();
                fileName = $"{tmpName} ({i}).{tmpExt}";
                i++;
            }

            var createdFile = Path.Combine(_explorer.ExchangePath.FullName, fileName);
            using (var stream = new FileStream(createdFile, FileMode.Create))
            {
                file.AddonFile.CopyTo(stream);
            }
            status = "上传文件成功";

            System.IO.File.SetLastWriteTime(createdFile, DateTime.Now);
            string tmpStr = "";
            if (ext == "zip")
            {
                var list = ZipHelper.ExtractZipFile(createdFile, "", _explorer.ExchangePath.FullName);
                if (_explorer.AddonPath != null)
                {
                    foreach (var decompressed in list)
                    {
                        System.IO.File.Move(decompressed, Path.Combine(_explorer.AddonPath.FullName, new FileInfo(decompressed).Name));
                    }
                    tmpStr = "，并已自动解压vpk至addons目录";
                }

                if (list.Count == 0)
                {
                    tmpStr = "，但压缩包内并无有效文件";
                }
            }
            else if (ext == "vpk")
            {
                if (_explorer.AddonPath != null)
                {
                    System.IO.File.Move(createdFile, Path.Combine(_explorer.AddonPath.FullName, new FileInfo(createdFile).Name));
                    tmpStr = "，并已自动移动至addons目录";
                }
            }

            TempData["Message"] = $"{status}：{fileName}{tmpStr}。";
        }
        catch (Exception ex)
        {
            TempData["Message"] = $"{status}：{ex.Message}";
        }

        return RedirectToAction("Index");
    }

    private void NavigateToLastFolder()
    {
        var lastFolder = HttpContext.Session.GetString(SessionKeyCurrentFolder);
        if (lastFolder != null)
            _explorer.NavigateRelativeFolder(lastFolder);
    }

}