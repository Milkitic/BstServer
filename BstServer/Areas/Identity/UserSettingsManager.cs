using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace BstServer.Areas.Identity;

public class UserSettingsManager
{
    private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "IdentitySettings.json");

    ObservableCollection<UserSettings> UserSettings { get; set; }

    public UserSettingsManager()
    {
        if (!File.Exists(configPath))
        {
            UserSettings = new ObservableCollection<UserSettings>();
        }
        else
            UserSettings = JsonConvert.DeserializeObject<ObservableCollection<UserSettings>>(File.ReadAllText(configPath));
        UserSettings.CollectionChanged += UserSettings_CollectionChanged;
        SaveToFile();
    }

    public void SaveToFile()
    {
        File.WriteAllText(configPath, JsonConvert.SerializeObject(UserSettings));
    }

    public void AddUser(string userId)
    {
        UserSettings.Add(new UserSettings
        {
            UserId = userId
        });
    }

    public bool TryGetSettings(string userId, out UserSettings settings)
    {
        settings = UserSettings.FirstOrDefault(k => k.UserId == userId);
        return settings != null;
    }

    private void UserSettings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        SaveToFile();
    }
}