namespace CoreLib.Data.Configuration {
 public class ConfigEntry<T> { public T Value {get;set;} }
 public class ConfigFile {
  public ConfigFile(string path,bool save){}
  public bool SaveOnConfigSet {get;set;}
  public ConfigEntry<T> Bind<T>(string section,string key,T value) => new ConfigEntry<T>();
  public void Reload(){}
 }
}
