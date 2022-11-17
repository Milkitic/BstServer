using Milkitic.FileExplorer.Views;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BstServer.Areas.Admin.Models;

public class JsonModelBase
{
    public JsonModelBase(int code, string status, string message, object data)
    {
        Code = code;
        Status = status;
        Message = message;
        Data = data;
    }

    [JsonProperty("code")]
    public int Code { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; }
    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public object Data { get; set; }
}

public class ExplorerJsonModel
{
    public ExplorerJsonModel(string currentPath, bool canUpload, List<ViewCell> files)
    {
        CurrentPath = currentPath;
        CanUpload = canUpload;
        Files = files;
    }
    [JsonProperty("current_path")]
    public string CurrentPath { get; set; }
    [JsonProperty("can_upload")]
    public bool CanUpload { get; set; }
    [JsonProperty("files")]
    public List<ViewCell> Files { get; set; }
}